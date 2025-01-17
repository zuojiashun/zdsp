﻿#if WSA_PLUGIN

using System.Collections.Generic;

namespace UTNotifications.WSA
{
    public sealed class ReceivedNotification
    {
        public ReceivedNotification(string title, string text, int id, IDictionary<string, string> userData, string notificationProfile)
        {
            this.title = title;
            this.text = text;
            this.id = id;
            this.userData = userData;
            this.notificationProfile = notificationProfile;
        }

        public string Title
        {
            get
            {
                return title;
            }
        }

        public string Text
        {
            get
            {
                return text;
            }
        }

        public int Id
        {
            get
            {
                return id;
            }
        }

        public IDictionary<string, string> UserData
        {
            get
            {
                return userData;
            }
        }

        public string NotificationProfile
        {
            get
            {
                return notificationProfile;
            }
        }

        private readonly string title;
        private readonly string text;
        private readonly int id;
        private readonly IDictionary<string, string> userData;
        private readonly string notificationProfile;
    }
}

#endif