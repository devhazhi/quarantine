using FirebaseAdmin;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace service.Utils
{
    public class NotificationManager
    {
        private FirebaseMessaging _firebaseMessaging;
        public readonly static NotificationManager Instance = new NotificationManager();
        private NotificationManager()
        {
            var defaultApp = FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile("quarantinemap-e57f2952a943.json"),
            });
            Console.WriteLine(defaultApp.Name); // "[DEFAULT]"

            // Retrieve services by passing the defaultApp variable...
            var defaultAuth = FirebaseAuth.GetAuth(defaultApp);
            _firebaseMessaging = FirebaseAdmin.Messaging.FirebaseMessaging.GetMessaging(defaultApp);
        }

        public Task<string> SendNotification(string topic, string title, string body)
        {
            return _firebaseMessaging.SendAsync(new FirebaseAdmin.Messaging.Message()
            {
                Android = new FirebaseAdmin.Messaging.AndroidConfig()
                {
                    Notification = new FirebaseAdmin.Messaging.AndroidNotification()
                    {
                        Title = title,
                        Body = body
                    },
                    TimeToLive = TimeSpan.FromMinutes(1),
                    Priority = FirebaseAdmin.Messaging.Priority.High,
                    RestrictedPackageName = "devhazhi.firebase.test"
                },
                Topic = topic

            });
        }
    }
}
