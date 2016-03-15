using Newtonsoft.Json;
using SteamAuth;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using System;

namespace SteamAppNative
{
    class Storage
    {
        internal static SteamGuardAccount SGAFromStore(string username)
        {
            SteamGuardAccount response = null;

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("steamUser-" + username))
            {
                response = SGAFromStore((ulong)localSettings.Values["steamUser-" + username]);
            }
            else if (localSettings.Values.ContainsKey("steamUser-")) // Empty AccountName
            {
                response = SGAFromStore((ulong)localSettings.Values["steamUser-"]);
                PushStore(response); // Push it back correctly
            }

            return response;
        }

        internal static SteamGuardAccount SGAFromStore(ulong steamid)
        {
            SteamGuardAccount response = null;

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
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
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["steamGuard-" + account.Session.SteamID] = JsonConvert.SerializeObject(account);
            localSettings.Values["steamUser-" + account.AccountName] = account.Session.SteamID;
        }

        internal static void SetCurrentUser(ulong steamid)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["currentAccount"] = steamid;
        }

        internal static SessionData SDFromStore()
        {
            Dictionary<ulong, SessionData> response = GetAccounts();

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("currentAccount"))
            {
                // Try and find current account in accounts list
                // Fall back to first account in list
                ulong currentAcc = (ulong)localSettings.Values["currentAccount"];
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

        internal static Dictionary<ulong, SessionData> GetAccounts()
        {
            Dictionary<ulong, SessionData> response = new Dictionary<ulong, SessionData>();

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("sessionJson"))
            {
                string data = (string)localSettings.Values["sessionJson"];
                try {
                    response = JsonConvert.DeserializeObject<Dictionary<ulong, SessionData>>(data);
                } catch {
                    // Handle old single session data
                    SessionData session = JsonConvert.DeserializeObject<SessionData>(data);
                    response[session.SteamID] = session;
                }        
            }

            return response;
        }

        internal static void PushStore(SessionData session)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            Dictionary<ulong, SessionData> accounts = GetAccounts();

            accounts[session.SteamID] = session;

            localSettings.Values["sessionJson"] = JsonConvert.SerializeObject(accounts);
            SetCurrentUser(session.SteamID);
        }

        internal static void SDLogout()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            if (localSettings.Values.ContainsKey("currentAccount")) {
                ulong currentAcc = (ulong)localSettings.Values["currentAccount"];

                Dictionary<ulong, SessionData> accounts = GetAccounts();
                accounts.Remove(currentAcc);

                localSettings.Values["sessionJson"] = JsonConvert.SerializeObject(accounts);
                localSettings.Values.Remove("currentAccount");
            }
        }
    }
}
