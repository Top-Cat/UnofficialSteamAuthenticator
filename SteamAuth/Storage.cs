using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using Newtonsoft.Json;
using UnofficialSteamAuthenticator.Lib.SteamAuth;

namespace UnofficialSteamAuthenticator.Lib
{
    /// <summary>
    ///     Handles storing and retreiving user data
    /// </summary>
    public class Storage
    {
        /// <summary>
        ///     Gets steam guard account information by username
        ///     Used to generate codes when re-logging in before the user's steamid is known
        /// </summary>
        /// <param name="username">Steam account username of user</param>
        /// <returns>The SteamGuardAccount of the user or null if one cannot be found</returns>
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

        /// <summary>
        ///     Gets steam guard account information by steamid
        ///     Used to get the secrets of enrolled, logged in users
        /// </summary>
        /// <param name="steamId">The steamId of the user</param>
        /// <returns>The SteamGuardAccount of the user, an empty object may be returned if none is present</returns>
        public static SteamGuardAccount GetSteamGuardAccount(ulong steamId)
        {
            SteamGuardAccount response = new SteamGuardAccount();

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("steamGuard-" + steamId))
            {
                var data = (string) localSettings.Values["steamGuard-" + steamId];
                response = JsonConvert.DeserializeObject<SteamGuardAccount>(data);
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

        /// <summary>
        ///     Gets steam guard account information of the current active user
        ///     Most commonly used to prevent the need to pass a steamid everywhere
        /// </summary>
        /// <returns>The SteamGuardAccount of the user or null if no users are logged in</returns>
        public static SteamGuardAccount GetSteamGuardAccount()
        {
            SessionData session = GetSessionData();
            if (session == null)
                return null;

            SteamGuardAccount response = GetSteamGuardAccount(session.SteamID);
            response.Session = session;

            return response;
        }

        /// <summary>
        ///     Commits a SteamGuardAccount to storage
        ///     Make sure to call this after making any changes to the account that should persist
        /// </summary>
        /// <param name="account">The account to save</param>
        public static void PushStore(SteamGuardAccount account)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["steamGuard-" + account.Session.SteamID] = JsonConvert.SerializeObject(account);
            localSettings.Values["steamUser-" + account.AccountName] = account.Session.SteamID;
        }

        /// <summary>
        ///     Set the current active user
        ///     Called by the user selection screen before forwarding into single user mode
        ///     Also updates the LastCurrent time of the user, used for storting
        /// </summary>
        /// <param name="steamid">The steamid of the user to set active</param>
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

        /// <summary>
        ///     Get the session data of the current active user
        ///     Session data is a simple login before authenticator linking with keys to call the apis
        /// </summary>
        /// <returns>The current user's session data</returns>
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

            return response.Count > 0 ? response.First().Value : null;
        }

        /// <summary>
        ///     Get all the logged in accounts
        ///     Used to display the user list
        /// </summary>
        /// <returns>A dictionary of logged in accounts steamid => SessionData</returns>
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

        /// <summary>
        ///     Commits SessionData to storage
        ///     Make sure to call this after making any changes to the session that should persist
        /// </summary>
        /// <param name="session">The session to save</param>
        public static void PushStore(SessionData session)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            Dictionary<ulong, SessionData> accounts = GetAccounts();

            accounts[session.SteamID] = session;

            localSettings.Values["sessionJson"] = JsonConvert.SerializeObject(accounts);
        }

        /// <summary>
        ///     Deletes the session data for a user by steamId
        ///     Does not remove any SteamGuardAccount information
        /// </summary>
        /// <param name="steamid">The steamId of the user to logout</param>
        public static void Logout(ulong steamid)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            Dictionary<ulong, SessionData> accounts = GetAccounts();
            accounts.Remove(steamid);

            localSettings.Values["sessionJson"] = JsonConvert.SerializeObject(accounts);
        }

        /// <summary>
        ///     Logs out the current active user
        /// </summary>
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
