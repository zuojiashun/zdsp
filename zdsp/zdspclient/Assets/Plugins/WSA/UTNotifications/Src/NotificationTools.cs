﻿#if WSA_PLUGIN

using System;
using System.Collections.Generic;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.Networking.PushNotifications;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using UTNotifications.NotificationsExtensions.ToastContent;

namespace UTNotifications.WSA
{
    public interface IInitializeHandler
    {
        void OnInitialized(string registrationId);
    }

    public sealed class NotificationTools
    {
    //public
        public static bool Initialize(bool willHandleReceivedNotifications, int startId, bool incrementalId, IInitializeHandler handler, bool pushEnabled, bool dontShowWhenRunning)
        {
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values[HANDLE_NOTIFICATIONS_KEY] = willHandleReceivedNotifications;
            settings.Values[NEXT_ID_KEY] = startId;
            settings.Values[INCREMENTAL_ID_KEY] = incrementalId;
            settings.Values[DONT_SHOW_WHEN_RUNNING_KEY] = dontShowWhenRunning;

            PackageVersion packageVersion = Package.Current.Id.Version;
            string appVersion = packageVersion.Major + "." + packageVersion.Minor + "." + packageVersion.Revision + "." + packageVersion.Build;

            RegisterBackgroundTasks(appVersion, pushEnabled);
            if (pushEnabled)
            {
                CreateWNSChannel(handler);
            }

            return true;
        }

        public static void PostLocalNotification(string title, string text, int id, IDictionary<string, string> userData, string notificationProfile)
        {
            ScheduleNotification(1, title, text, id, userData, notificationProfile);
        }

        public static void ScheduleNotification(int triggerInSeconds, string title, string text, int id, IDictionary<string, string> userData, string notificationProfile)
        {
            if (triggerInSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException("triggerInSeconds");
            }
            
            CancelNotification(id);

            ScheduleSingleNotification(triggerInSeconds, title, text, id, notificationProfile);
            RegisterNotification(triggerInSeconds, 0, title, text, id, userData, notificationProfile, 0);
        }

        public static void ScheduleNotificationRepeating(int firstTriggerInSeconds, int intervalSeconds, string title, string text, int id, IDictionary<string, string> userData, string notificationProfile)
        {
            if (firstTriggerInSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException("firstTriggerInSeconds");
            }

            if (intervalSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException("intervalSeconds");
            }

            int triggerInSeconds;
            bool enabled = NotificationsEnabled();
            if (enabled)
            {
                int scheduleInAdvanceSeconds = Math.Min(ScheduleRepeatedForSeconds + intervalSeconds, MaxRepeatsSchedulingAtOnce * intervalSeconds);

                IToastNotificationContent notificationContent = PrepareNotification(title, text, id, notificationProfile);
                int minTriggerInSecondsAllowed = GetMinTriggerInSecondsAllowed();

                for (triggerInSeconds = firstTriggerInSeconds; triggerInSeconds < firstTriggerInSeconds + scheduleInAdvanceSeconds; triggerInSeconds += intervalSeconds)
                {
                    ScheduleSingleNotification(triggerInSeconds, id, notificationContent, minTriggerInSecondsAllowed, true);
                }
            }
            else
            {
                triggerInSeconds = firstTriggerInSeconds - intervalSeconds;
            }

            RegisterNotification(triggerInSeconds, intervalSeconds, title, text, id, userData, notificationProfile, GetCurrentUnixTimeSeconds() + firstTriggerInSeconds - intervalSeconds - 1);
        }

        public static void Reschedule(bool onEnabled)
        {
            bool enabled = NotificationsEnabled();
            if (enabled)
            {
                var settings = ApplicationData.Current.LocalSettings;
                foreach (KeyValuePair<string, object> it in settings.Values)
                {
                    if (it.Key.StartsWith(STORE_KEY_PREFIX))
                    {
                        Reschedule((ApplicationDataCompositeValue)it.Value, onEnabled);
                    }
                }
            }
        }

        public static void CancelNotification(int id)
        {
            string strId = "id" + id;
            ToastNotifier toastNotifier = ToastNotificationManager.CreateToastNotifier();
            foreach (var it in toastNotifier.GetScheduledToastNotifications())
            {
                if (it.Id == strId)
                {
                    toastNotifier.RemoveFromSchedule(it);
                }
            }

            UnregisterNotification(id);
        }

        public static void CancelAllNotifications()
        {
            ToastNotifier toastNotifier = ToastNotificationManager.CreateToastNotifier();
            foreach (var it in toastNotifier.GetScheduledToastNotifications())
            {
                toastNotifier.RemoveFromSchedule(it);
            }

            var settings = ApplicationData.Current.LocalSettings;
            foreach (var it in new List<string>(settings.Values.Keys))
            {
                if (it.StartsWith(STORE_KEY_PREFIX))
                {
                    settings.Values.Remove(it);
                }
            }
        }

        public static void SetNotificationsEnabled(bool enabled)
        {
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values[DISABLED_KEY] = !enabled;

            if (!enabled)
            {
                ToastNotifier toastNotifier = ToastNotificationManager.CreateToastNotifier();
                foreach (var it in toastNotifier.GetScheduledToastNotifications())
                {
                    toastNotifier.RemoveFromSchedule(it);
                }

                long currentTime = GetCurrentUnixTimeSeconds();
                foreach (var it in new List<string>(settings.Values.Keys))
                {
                    if (it.StartsWith(STORE_KEY_PREFIX))
                    {
                        ApplicationDataCompositeValue registeredNotification = (ApplicationDataCompositeValue)settings.Values[it];
                        int intervalSeconds = (int)registeredNotification["intervalSeconds"];
                        if (intervalSeconds > 0)
                        {
                            //Repeated
                            int lastTriggerInSecondsFromNow = (int)((long)registeredNotification["lastTriggerInSeconds"] - currentTime);
                            lastTriggerInSecondsFromNow %= intervalSeconds;
                            if (lastTriggerInSecondsFromNow > 0)
                            {
                                lastTriggerInSecondsFromNow -= intervalSeconds;
                            }

                            registeredNotification["lastTriggerInSeconds"] = currentTime + lastTriggerInSecondsFromNow;
                            settings.Values[it] = registeredNotification;
                        }
                    }
                }
            }
            else
            {
                Reschedule(true);
            }
        }

        public static bool NotificationsEnabled()
        {
            var settings = ApplicationData.Current.LocalSettings;
            return settings.Values.ContainsKey(DISABLED_KEY) ? !(bool)settings.Values[DISABLED_KEY] : true;
        }

        public static int GetNextPushNotificationId()
        {
            var settings = ApplicationData.Current.LocalSettings;
            int nextId = settings.Values.ContainsKey(NEXT_ID_KEY) ? (int)settings.Values[NEXT_ID_KEY] : 0;
            bool incrementalId = settings.Values.ContainsKey(INCREMENTAL_ID_KEY) ? (bool)settings.Values[INCREMENTAL_ID_KEY] : false;

            if (incrementalId)
            {
                settings.Values[NEXT_ID_KEY] = nextId + 1;
            }

            return nextId;
        }

        public static void UpdateWhenRunning()
        {
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values[LAST_TIME_UPDATED_WHEN_RUNNING_KEY] = GetCurrentUnixTimeSeconds();

            if (NotificationsEnabled())
            {
                DateTime minTime = DateTime.Now.AddSeconds(CancelInAdvanceSecondsIfDontShowWhenRunning);

                if (settings.Values.ContainsKey(DONT_SHOW_WHEN_RUNNING_KEY) && (bool)settings.Values[DONT_SHOW_WHEN_RUNNING_KEY])
                {
                    ToastNotifier toastNotifier = ToastNotificationManager.CreateToastNotifier();
                    foreach (var it in toastNotifier.GetScheduledToastNotifications())
                    {
                        if (it.DeliveryTime < minTime)
                        {
                            toastNotifier.RemoveFromSchedule(it);
                        }
                    }
                }
            }
        }

        public static void HandleReceivedNotifications(string appArguments, out IList<ReceivedNotification> allReceivedNotifications, out ReceivedNotification clickedNotification)
        {
            allReceivedNotifications = null;
            clickedNotification = null;

            if (ApplicationData.Current == null)
            {
                return;
            }

            var settings = ApplicationData.Current.LocalSettings;
            if (settings == null || settings.Values == null)
            {
                return;
            }

            clickedNotification = GetClickedNotification(appArguments, settings);

            long currentTime = GetCurrentUnixTimeSeconds();

            if (HandleNotifications() && NotificationsEnabled())
            {
                foreach (var it in new List<string>(settings.Values.Keys))
                {
                    if (it.StartsWith(STORE_KEY_PREFIX))
                    {
                        ApplicationDataCompositeValue registeredNotification = (ApplicationDataCompositeValue)settings.Values[it];
                        if (registeredNotification == null)
                        {
                            continue;
                        }

                        long lastTriggerInSeconds = (long)registeredNotification["lastTriggerInSeconds"];
                        long lastTimeHandled = (long)registeredNotification["lastTimeHandled"];
                        int intervalSeconds = (int)registeredNotification["intervalSeconds"];

                        bool handle;
                        if (intervalSeconds > 0)
                        {
                            //Repeated
                            handle = (currentTime - lastTimeHandled > intervalSeconds);
                        }
                        else
                        {
                            //One shot
                            handle = (currentTime > lastTriggerInSeconds && lastTimeHandled != (long)(-1));
                        }

                        if (handle)
                        {
                            if (allReceivedNotifications == null)
                            {
                                allReceivedNotifications = new List<ReceivedNotification>();
                            }

                            allReceivedNotifications.Add(ReadReceivedNotification(registeredNotification));

                            if (intervalSeconds > 0)
                            {
                                //Repeated
                                long newLastTimeHandled = (lastTriggerInSeconds - currentTime) % intervalSeconds;
                                if (newLastTimeHandled > 0)
                                {
                                    newLastTimeHandled -= intervalSeconds;
                                }
                                newLastTimeHandled += currentTime;
                                registeredNotification["lastTimeHandled"] = newLastTimeHandled;
                                settings.Values[it] = registeredNotification;
                            }
                            else
                            {
                                //One shot
                                registeredNotification["lastTimeHandled"] = (long)(-1);
                            }

                            settings.Values[it] = registeredNotification;
                        }
                    }
                }
            }
        }

    //private
        private static async void RegisterBackgroundTasks(string appVersion, bool pushEnabled)
        {
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(VERSION_STORE_KEY) || (string)ApplicationData.Current.LocalSettings.Values[VERSION_STORE_KEY] != appVersion)
            {
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey(VERSION_STORE_KEY))
                {
                    BackgroundExecutionManager.RemoveAccess();
                }

                if (await BackgroundExecutionManager.RequestAccessAsync() != BackgroundAccessStatus.Denied)
                {
                    ApplicationData.Current.LocalSettings.Values[VERSION_STORE_KEY] = appVersion;
                }
            }

            BackgroundTask.Register();
            if (pushEnabled)
            {
                PushBackgroundTask.Register();
            }
        }

        private static async void CreateWNSChannel(IInitializeHandler handler)
        {
            PushNotificationChannel channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            channel.PushNotificationReceived += OnPushNotificationReceived;
            handler.OnInitialized(channel.Uri);
        }

        private static void RegisterNotification(int lastTriggerInSecondsFromNow, int intervalSeconds, string title, string text, int id, IDictionary<string, string> userData, string notificationProfile, long lastTimeHandled)
        {
            ApplicationDataCompositeValue registeredNotification = new ApplicationDataCompositeValue();
            registeredNotification["lastTriggerInSeconds"] = GetCurrentUnixTimeSeconds() + (long)lastTriggerInSecondsFromNow;
            registeredNotification["intervalSeconds"] = intervalSeconds;
            registeredNotification["title"] = title;
            registeredNotification["text"] = text;
            registeredNotification["id"] = id;
            PackUserData(registeredNotification, userData);
            registeredNotification["notificationProfile"] = notificationProfile;
            registeredNotification["lastTimeHandled"] = lastTimeHandled;

            var settings = ApplicationData.Current.LocalSettings;
            settings.Values[STORE_KEY_PREFIX + id] = registeredNotification;
        }

        private static void UnregisterNotification(int id)
        {
            var settings = ApplicationData.Current.LocalSettings;
            string key = STORE_KEY_PREFIX + id;
            if (settings.Values.ContainsKey(key))
            {
                settings.Values.Remove(key);
            }
        }

        private static void Reschedule(ApplicationDataCompositeValue registeredNotification, bool onEnabled)
        {
            int intervalSeconds = (int)registeredNotification["intervalSeconds"];
            if (intervalSeconds <= 0 && !onEnabled)
            {
                //Not repeating
                return;
            }

            long currentTime = GetCurrentUnixTimeSeconds();

            int lastTriggerInSecondsFromNow = (int)((long)registeredNotification["lastTriggerInSeconds"] - currentTime);
            int scheduleInAdvanceSeconds = Math.Min(ScheduleRepeatedForSeconds + intervalSeconds, MaxRepeatsSchedulingAtOnce * intervalSeconds);
            string title = (string)registeredNotification["title"];
            string text = (string)registeredNotification["text"];
            int id = (int)registeredNotification["id"];
            IDictionary<string, string> userData = UnpackUserData(registeredNotification);
            string notificationProfile = (string)registeredNotification["notificationProfile"];
            long lastTimeHandled = onEnabled ? currentTime : (long)registeredNotification["lastTimeHandled"];

            if (intervalSeconds > 0)
            {
                //Repeating
                if (lastTriggerInSecondsFromNow + intervalSeconds < scheduleInAdvanceSeconds)
                {
                    if (lastTriggerInSecondsFromNow < 0)
                    {
                        lastTriggerInSecondsFromNow %= intervalSeconds;
                    }

                    IToastNotificationContent notificationContent = PrepareNotification(title, text, id, notificationProfile);
                    int minTriggerInSecondsAllowed = GetMinTriggerInSecondsAllowed();

                    int triggerInSeconds;
                    for (triggerInSeconds = lastTriggerInSecondsFromNow + intervalSeconds; triggerInSeconds < scheduleInAdvanceSeconds; triggerInSeconds += intervalSeconds)
                    {
                        ScheduleSingleNotification(triggerInSeconds, id, notificationContent, minTriggerInSecondsAllowed, true);
                    }

                    RegisterNotification(triggerInSeconds, intervalSeconds, title, text, id, userData, notificationProfile, lastTimeHandled);
                }
            }
            else
            {
                //One shot
                if (lastTriggerInSecondsFromNow > 0)
                {
                    ScheduleSingleNotification(lastTriggerInSecondsFromNow, title, text, id, notificationProfile);
                }
                else
                {
                    UnregisterNotification(id);
                }
            }
        }

        private static IToastNotificationContent PrepareNotification(string title, string text, int id, string notificationProfile)
        {
            IToastText02 toastContent = ToastContentFactory.CreateToastText02();
            toastContent.TextHeading.Text = title;
            toastContent.TextBodyWrap.Text = text;
            toastContent.Launch = "-notification=" + id.ToString();

            return toastContent;
        }

        private static void ScheduleSingleNotification(int triggerInSeconds, string title, string text, int id, string notificationProfile)
        {
            IToastNotificationContent notificationContent = PrepareNotification(title, text, id, notificationProfile);
            ScheduleSingleNotification(triggerInSeconds, id, notificationContent, GetMinTriggerInSecondsAllowed(), NotificationsEnabled());
        }

        private static void ScheduleSingleNotification(int triggerInSeconds, int id, IToastNotificationContent notificationContent, int minTriggerInSecondsAllowed, bool enabled)
        {
            if (enabled && triggerInSeconds >= minTriggerInSecondsAllowed)
            {
                DateTime dt = DateTime.Now.AddSeconds(triggerInSeconds);
                string launchArgs = notificationContent.Launch;
                notificationContent.Launch = launchArgs + "." + dt.ToFileTimeUtc();
                ScheduledToastNotification notification = new ScheduledToastNotification(notificationContent.GetXml(), dt);
                notificationContent.Launch = launchArgs;
                notification.Id = "id" + id;
                ToastNotificationManager.CreateToastNotifier().AddToSchedule(notification);
            }
        }

        private static void PackUserData(ApplicationDataCompositeValue packTo, IDictionary<string, string> userData)
        {
            if (userData != null)
            {
                foreach (KeyValuePair<string, string> it in userData)
                {
                    packTo["userData." + it.Key] = it.Value;
                }
            }
        }

        private static IDictionary<string, string> UnpackUserData(ApplicationDataCompositeValue packFrom)
        {
            IDictionary<string, string> userData = null;

            string prefix = "userData.";
            int prefixLength = prefix.Length;

            foreach (KeyValuePair<string, object> it in packFrom)
            {
                if (it.Key.StartsWith(prefix))
                {
                    if (userData == null)
                    {
                        userData = new Dictionary<string, string>();
                    }

                    userData[it.Key.Substring(prefixLength)] = (string)it.Value;
                }
            }

            return userData;
        }

        private static long GetCurrentUnixTimeSeconds()
        {
            return (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        private static long GetLastTimeUpdatedWhenRunning()
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (settings.Values.ContainsKey(LAST_TIME_UPDATED_WHEN_RUNNING_KEY))
            {
                return (long)settings.Values[LAST_TIME_UPDATED_WHEN_RUNNING_KEY];
            }
            else
            {
                return GetCurrentUnixTimeSeconds();
            }
        }

        private static int GetMinTriggerInSecondsAllowed()
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (settings.Values.ContainsKey(DONT_SHOW_WHEN_RUNNING_KEY) && (bool)settings.Values[DONT_SHOW_WHEN_RUNNING_KEY])
            {
                return (int)(GetLastTimeUpdatedWhenRunning() + CancelInAdvanceSecondsIfDontShowWhenRunning - GetCurrentUnixTimeSeconds());
            }
            else
            {
                return 0;
            }
        }

        private static bool HandleNotifications()
        {
            var settings = ApplicationData.Current.LocalSettings;
            return settings.Values.ContainsKey(HANDLE_NOTIFICATIONS_KEY) ? (bool)settings.Values[HANDLE_NOTIFICATIONS_KEY] : false;
        }

        private static ReceivedNotification GetClickedNotification(string appArguments, ApplicationDataContainer settings)
        {
            if (string.IsNullOrEmpty(appArguments))
            {
                return null;
            }

            const string argument = "-notification=";
            int index = appArguments.IndexOf(argument);
            if (index < 0)
            {
                return null;
            }

            index += argument.Length;
            int length = 0;
            for (int i = index; i < appArguments.Length; ++i)
            {
                if (char.IsWhiteSpace(appArguments[i]))
                {
                    break;
                }
                else
                {
                    ++length;
                }
            }

            string argValue = appArguments.Substring(index, length);
            if (m_lastClickedNotification == argValue)
            {
                return null;
            }

            m_lastClickedNotification = argValue;

            int pointIndex = argValue.IndexOf('.');
            if (pointIndex < 0)
            {
                return null;
            }

            string id = argValue.Substring(0, pointIndex);
            string key = STORE_KEY_PREFIX + id;
            if (!settings.Values.ContainsKey(key))
            {
                return null;
            }

            return ReadReceivedNotification((ApplicationDataCompositeValue)settings.Values[key]);
        }

        private static ReceivedNotification ReadReceivedNotification(ApplicationDataCompositeValue registeredNotification)
        {
            if (registeredNotification != null)
            {
                return new ReceivedNotification((string)registeredNotification["title"], (string)registeredNotification["text"], (int)registeredNotification["id"], UnpackUserData(registeredNotification), (string)registeredNotification["notificationProfile"]);
            }
            else
            {
                return null;
            }
        }

        private static void OnPushNotificationReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs e)
        {
            PostLocalNotification("Push", e.RawNotification.Content, 0, null, null);
        }

        private static string m_lastClickedNotification = null;

        private const int MaxRepeatsSchedulingAtOnce = 15;
        private const int ScheduleRepeatedForSeconds = 60 * 60 * 24 * 3;    //Schedule for 3 days in advance
        private const int CancelInAdvanceSecondsIfDontShowWhenRunning = 10;
        private static readonly string STORE_KEY_PREFIX = "__UTNotifications_.registration.";
        private static readonly string VERSION_STORE_KEY = "__UTNotifications_.appversion";
        private static readonly string HANDLE_NOTIFICATIONS_KEY = "__UTNotifications_.handle_notifications";
        private static readonly string NEXT_ID_KEY = "__UTNotifications_.next_id";
        private static readonly string INCREMENTAL_ID_KEY = "__UTNotifications_.incremental_id";
        private static readonly string DONT_SHOW_WHEN_RUNNING_KEY = "__UTNotifications_.dont_show_when_running";
        private static readonly string LAST_TIME_UPDATED_WHEN_RUNNING_KEY = "__UTNotifications_.last_time_updated_when_running";
        private static readonly string DISABLED_KEY = "__UTNotifications_.disabled";
    }
}
#endif