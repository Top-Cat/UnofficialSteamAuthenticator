using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using UnofficialSteamAuthenticator.Lib;
using UnofficialSteamAuthenticator.Lib.SteamAuth;

namespace UnofficalSteamAuthenticator.NotificationTask
{
    public sealed class BackgroundTask : IBackgroundTask
    {
        private const string TaskName = "usa.notification";

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            var web = new SteamWeb();
            var locks = new List<WaitHandle>();
            var notifCount = 0;

            foreach (ulong steamid in Storage.GetAccounts().Keys)
            {
                SteamGuardAccount acc = Storage.GetSteamGuardAccount(steamid);
                if (!acc.FullyEnrolled)
                    continue;

                var lck = new AutoResetEvent(false);
                locks.Add(lck);
                acc.FetchConfirmations(web, (response, ex) =>
                {
                    if (ex != null)
                    {
                        acc.RefreshSession(web, success =>
                        {
                            if (success == Success.Failure)
                            {
                                Storage.Logout(acc.Session.SteamID);
                            }
                            lck.Set();
                        });
                        return;
                    }

                    foreach (Confirmation c in response.Where(c => c.ID > acc.NotifySince))
                    {
                        ShowNotification(steamid, c.ID.ToString(), c.Description, c.Description2);
                    }
                    notifCount += response.Count;
                    acc.NotifySince = response.DefaultIfEmpty().Max(x => x?.ID ?? 0);
                    acc.PushStore();

                    lck.Set();
                });
            }

            if (locks.Count > 0)
            {
                WaitHandle.WaitAll(locks.ToArray());
            }
            SetBadgeCount(notifCount);
        }

        public static async void Register()
        {
            BackgroundAccessStatus result = await BackgroundExecutionManager.RequestAccessAsync();

            if (result == BackgroundAccessStatus.Denied)
                return;

            KeyValuePair<Guid, IBackgroundTaskRegistration> task = BackgroundTaskRegistration.AllTasks.FirstOrDefault(x => x.Value.Name == TaskName);
            task.Value?.Unregister(true);

            var taskBuilder = new BackgroundTaskBuilder
            {
                Name = TaskName,
                TaskEntryPoint = "UnofficalSteamAuthenticator.NotificationTask.BackgroundTask"
            };
            taskBuilder.SetTrigger(new TimeTrigger(15, false));
            taskBuilder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
            taskBuilder.Register();
        }

        public static void SetBadgeCount(int c)
        {
            XmlDocument badgeXml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);
            var badgeAttributes = (XmlElement) badgeXml.GetElementsByTagName("badge")[0];
            badgeAttributes.SetAttribute("value", c.ToString());

            var badgeNotification = new BadgeNotification(badgeXml);
            BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(badgeNotification);
        }

        private static void ShowNotification(ulong steamid, string id, string title, string content)
        {
            XmlDocument toast = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            XmlNodeList txtNodes = toast.GetElementsByTagName("text");
            txtNodes[0].AppendChild(toast.CreateTextNode(title));
            txtNodes[1].AppendChild(toast.CreateTextNode(content));

            var toastNode = (XmlElement) toast.SelectSingleNode("toast");
            toastNode?.SetAttribute("launch", steamid.ToString());

            var launchToast = new ToastNotification(toast)
            {
                Tag = id,
                Group = Util.ConvertToSteam3(steamid).ToString()
            };

            ToastNotifier toastNotifier = ToastNotificationManager.CreateToastNotifier();
            toastNotifier.Show(launchToast);
        }

        public static void RemoveUserNotifications(ulong steamid)
        {
            ToastNotificationManager.History.RemoveGroup(Util.ConvertToSteam3(steamid).ToString());
            SetBadgeCount(0);
        }
    }
}
