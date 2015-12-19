using Newtonsoft.Json;
using SteamAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace SteamAppNative
{
    class Storage
    {
        internal static SteamGuardAccount SGAFromStore(string username)
        {
            SteamGuardAccount response = null;

            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("steamUser-" + username))
            {
                response = SGAFromStore((ulong)localSettings.Values["steamUser-" + username]);
            }

            return response;
        }

        internal static SteamGuardAccount SGAFromStore(ulong steamid)
        {
            SteamGuardAccount response = null;

            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("steamGuard-" + steamid))
            {
                string data = (string)localSettings.Values["steamGuard-" + steamid];
                response = JsonConvert.DeserializeObject<SteamGuardAccount>(data);
            }
            else
            {
                response = new SteamGuardAccount();
            }

            return response;
        }

        internal static SteamGuardAccount SGAFromStore()
        {
            SteamGuardAccount response = null;

            SessionData session = SDFromStore();
            if (session != null)
            {
                response = SGAFromStore(session.SteamID);
                response.Session = session;
            }

            return response;
        }

        internal static void PushStore(SteamGuardAccount account)
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["steamGuard-" + account.Session.SteamID] = JsonConvert.SerializeObject(account);
            localSettings.Values["steamUser-" + account.Username] = account.Session.SteamID;
        }

        internal static SessionData SDFromStore()
        {
            SessionData response = null;

            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("sessionJson"))
            {
                string data = (string)localSettings.Values["sessionJson"];
                response = JsonConvert.DeserializeObject<SessionData>(data);
            }

            return response;
        }

        internal static void PushStore(SessionData session)
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["sessionJson"] = JsonConvert.SerializeObject(session);
        }

        internal static void SDClearStore()
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values.Remove("sessionJson");
        }
    }
}
