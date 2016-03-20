using Newtonsoft.Json;
using SteamAuth;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;

namespace UnofficialSteamAuthenticator
{
    public class Storage
    {
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

        public static SteamGuardAccount GetSteamGuardAccount(ulong steamId)
        {
            SteamGuardAccount response;

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("steamGuard-" + steamId))
            {
                string data = (string) localSettings.Values["steamGuard-" + steamId];
                response = JsonConvert.DeserializeObject<SteamGuardAccount>(data);
            }
            else
            {
                response = new SteamGuardAccount();
            }

            Dictionary<ulong, SessionData> accs = Storage.GetAccounts();
            if (accs.ContainsKey(steamId))
            {
                response.Session = accs[steamId];
            }

            return response;
        }

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
        }

        public static SessionData GetSessionData()
        {
            Dictionary<ulong, SessionData> response = GetAccounts();

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("currentAccount"))
            {
                // Try and find current account in accounts list
                // Fall back to first account in list
                ulong currentAcc = (ulong) localSettings.Values["currentAccount"];
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

        public static Dictionary<ulong, SessionData> GetAccounts()
        {
            Dictionary<ulong, SessionData> response = new Dictionary<ulong, SessionData>();

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("sessionJson"))
            {
                string data = (string) localSettings.Values["sessionJson"];
                try
                {
                    response = JsonConvert.DeserializeObject<Dictionary<ulong, SessionData>>(data);
                }
                catch
                {
                    // Handle old single session data
                    SessionData session = JsonConvert.DeserializeObject<SessionData>(data);
                    response[session.SteamID] = session;
                }
            }

            return response;
        }

        public static void PushStore(SessionData session)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            Dictionary<ulong, SessionData> accounts = GetAccounts();

            accounts[session.SteamID] = session;

            localSettings.Values["sessionJson"] = JsonConvert.SerializeObject(accounts);
            SetCurrentUser(session.SteamID);
        }

        public static void Logout()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            if (localSettings.Values.ContainsKey("currentAccount"))
            {
                ulong currentAcc = (ulong) localSettings.Values["currentAccount"];

                Dictionary<ulong, SessionData> accounts = GetAccounts();
                accounts.Remove(currentAcc);

                localSettings.Values["sessionJson"] = JsonConvert.SerializeObject(accounts);
                localSettings.Values.Remove("currentAccount");
            }
        }
    }
}
