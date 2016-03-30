using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using Newtonsoft.Json;
using UnofficialSteamAuthenticator.SteamAuth;

namespace UnofficialSteamAuthenticator
{
    public class Storage
    {
        ///<summary>
        ///Extract SteamGuardAccount object by Steam username from phone's storage.
        ///</summary>
        public static SteamGuardAccount GetSteamGuardAccount(string username)
        {
            SteamGuardAccount response = null;

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("steamUser-" + username))
            {
                response = GetSteamGuardAccount((ulong) localSettings.Values["steamUser-" + username]);
            }
            else if (localSettings.Values.ContainsKey("steamUser-")) // Empty AccountName
            {
                response = GetSteamGuardAccount((ulong) localSettings.Values["steamUser-"]);
                PushStore(response); // Push it back correctly
            }

            return response;
        }
        ///<summary>
        ///Extract SteamGuardAccount object by SteamID from phone's storage.
        ///</summary>
        public static SteamGuardAccount GetSteamGuardAccount(ulong steamId)
        {
            SteamGuardAccount response;

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("steamGuard-" + steamId))
            {
                var data = (string) localSettings.Values["steamGuard-" + steamId];
                response = JsonConvert.DeserializeObject<SteamGuardAccount>(data);
            }
            else
            {
                response = new SteamGuardAccount();
            }

            Dictionary<ulong, SessionData> accs = GetAccounts();
            if (accs.ContainsKey(steamId))
            {
                response.Session = accs[steamId];

                if (response.AccountName != null && response.Session.Username != response.AccountName)
                {
                    response.Session.Username = response.AccountName;
                }
            }

            return response;
        }
        ///<summary>
        ///Recieve SteamGuardAccount object for current session.
        ///</summary>
        public static SteamGuardAccount GetSteamGuardAccount()
        {
            SteamGuardAccount response = null;

            SessionData session = GetSessionData();
            if (session != null)
            {
                response = GetSteamGuardAccount(session.SteamID);
                response.Session = session;
            }

            return response;
        }
        ///<summary>
        ///Save SteamGuardAccount object to phone's storage.
        ///</summary>
        public static void PushStore(SteamGuardAccount account)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["steamGuard-" + account.Session.SteamID] = JsonConvert.SerializeObject(account);
            localSettings.Values["steamUser-" + account.AccountName] = account.Session.SteamID;
        }

        public static void SetCurrentUser(ulong steamid)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["currentAccount"] = steamid;

            SessionData data = GetSessionData();
            if (data == null)
                return;

            data.LastCurrent = Util.GetSystemUnixTime();
            PushStore(data);
        }
        ///<summary>
        ///Recieve Session object for current Account.
        ///</summary>
        public static SessionData GetSessionData()
        {
            Dictionary<ulong, SessionData> response = GetAccounts();

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("currentAccount"))
            {
                // Try and find current account in accounts list
                // Fall back to first account in list
                var currentAcc = (ulong) localSettings.Values["currentAccount"];
                if (response.ContainsKey(currentAcc))
                {
                    return response[currentAcc];
                }
            }

            if (response.Count > 0)
            {
                return response.First().Value;
            }
            return null;
        }
        ///<summary>
        ///Recieves 
        ///</summary>
        public static Dictionary<ulong, SessionData> GetAccounts()
        {
            var response = new Dictionary<ulong, SessionData>();

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            if (localSettings.Values.ContainsKey("sessionJson"))
            {
                var data = (string) localSettings.Values["sessionJson"];
                try
                {
                    response = JsonConvert.DeserializeObject<Dictionary<ulong, SessionData>>(data);
                }
                catch
                {
                    // Handle old single session data
                    var session = JsonConvert.DeserializeObject<SessionData>(data);
                    response[session.SteamID] = session;
                }
            }

            return response;
        }
        ///<summary>
        ///Save Session object data to phone's storage.
        ///</summary>
        public static void PushStore(SessionData session)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            Dictionary<ulong, SessionData> accounts = GetAccounts();

            accounts[session.SteamID] = session;

            localSettings.Values["sessionJson"] = JsonConvert.SerializeObject(accounts);
        }
        ///<summary>
        ///Deletes account from phone's storage.
        ///</summary>
        public static void Logout(ulong steamid)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            Dictionary<ulong, SessionData> accounts = GetAccounts();
            accounts.Remove(steamid);

            localSettings.Values["sessionJson"] = JsonConvert.SerializeObject(accounts);
        }
        ///<summary>
        ///Deletes current account from phone's storage.
        ///</summary>
        public static void Logout()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            if (!localSettings.Values.ContainsKey("currentAccount"))
                return;

            var currentAcc = (ulong) localSettings.Values["currentAccount"];
            Logout(currentAcc);
            localSettings.Values.Remove("currentAccount");
        }
    }
}
