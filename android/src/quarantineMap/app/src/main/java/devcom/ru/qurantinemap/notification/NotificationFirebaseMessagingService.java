/**
 * Copyright 2016 Google Inc. All Rights Reserved.
 * <p>
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * <p>
 * http://www.apache.org/licenses/LICENSE-2.0
 * <p>
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package devcom.ru.qurantinemap.notification;

import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.media.RingtoneManager;
import android.net.ConnectivityManager;
import android.net.NetworkInfo;
import android.net.Uri;
import android.os.Build;

import androidx.annotation.NonNull;
import androidx.core.app.NotificationCompat;
import android.util.Log;

import com.google.android.gms.tasks.OnCompleteListener;
import com.google.android.gms.tasks.Task;
import com.google.firebase.messaging.FirebaseMessaging;
import com.google.firebase.messaging.FirebaseMessagingService;
import com.google.firebase.messaging.RemoteMessage;

import androidx.work.OneTimeWorkRequest;
import androidx.work.WorkManager;

import java.util.Enumeration;
import java.util.Hashtable;
import java.util.Map;

import devcom.ru.qurantinemap.R;
import devcom.ru.qurantinemap.MapsActivity;
import devcom.ru.qurantinemap.api.models.Responce;
import devcom.ru.qurantinemap.service.DownloadCallback;
import devcom.ru.qurantinemap.service.DownloadTask;
import devcom.ru.qurantinemap.service.NetworkInfoCallback;
import devcom.ru.qurantinemap.service.RequestResult;
import devcom.ru.qurantinemap.service.ResultCallback;
import devcom.ru.qurantinemap.service.ServiceProxy;

/**
 * NOTE: There can only be one service in each app that receives FCM messages. If multiple
 * are declared in the Manifest then the first one will be chosen.
 *
 * In order to make this Java sample functional, you must remove the following from the Kotlin messaging
 * service in the AndroidManifest.xml:
 *
 * <intent-filter>
 *   <action android:name="com.google.firebase.MESSAGING_EVENT"/>
 * </intent-filter>
 */
public class NotificationFirebaseMessagingService extends FirebaseMessagingService implements NetworkInfoCallback {

    private static final String TAG = "NotificationFirebaseMessagingService";
    private static Hashtable<String, String> _dicSubscribeTopic  = new Hashtable<String, String>();


    private static void subscribeTopic(@NonNull final String[] topics) {
        Enumeration<String>  keys = _dicSubscribeTopic.keys();
        if(keys != null && keys.hasMoreElements()) {
            Hashtable<String, String> topicsHash = new Hashtable<String, String>();
            for (String key :
                    topics) {
                topicsHash.put(key, "");
            }
            while (keys.hasMoreElements()){
                String key = keys.nextElement();
                if(topicsHash.containsKey(key)) {
                    topicsHash.remove(key);
                }else{
                    FirebaseMessaging.getInstance().unsubscribeFromTopic(key);
                    _dicSubscribeTopic.remove(key);
                }
            }
        }
        for (final String topic :
                topics) {
            if (_dicSubscribeTopic.containsKey(topic) == false) {
                FirebaseMessaging.getInstance().subscribeToTopic(topic)
                        .addOnCompleteListener(
                                new OnCompleteListener<Void>() {
                                    @Override
                                    public void onComplete(@NonNull Task<Void> task) {
                                        if (task.isSuccessful())
                                            _dicSubscribeTopic.put(topic, "");
                                        else _dicSubscribeTopic.remove(topic);
                                    }
                                }
                        );
            } else {
                
            }
        }
    }
    private ServiceProxy serviceProxy;
    private ServiceProxy getServiceProxy(){
        if(serviceProxy==null)
            serviceProxy = new ServiceProxy(this);
        return serviceProxy;
    }
    /**
     * Called when message is received.
     *
     * @param remoteMessage Object representing the message received from Firebase Cloud Messaging.
     */
    // [START receive_message]
    @Override
    public void onMessageReceived(RemoteMessage remoteMessage) {
        // [START_EXCLUDE]
        // There are two types of messages data messages and notification messages. Data messages
        // are handled
        // here in onMessageReceived whether the app is in the foreground or background. Data
        // messages are the type
        // traditionally used with GCM. Notification messages are only received here in
        // onMessageReceived when the app
        // is in the foreground. When the app is in the background an automatically generated
        // notification is displayed.
        // When the user taps on the notification they are returned to the app. Messages
        // containing both notification
        // and data payloads are treated as notification messages. The Firebase console always
        // sends notification
        // messages. For more see: https://firebase.google.com/docs/cloud-messaging/concept-options
        // [END_EXCLUDE]

        // Not getting messages here? See why this may be: https://goo.gl/39bRNJ
        String formText = remoteMessage.getFrom();
        Log.d(TAG, "From: " +formText);

        // Check if message contains a data payload.
        if (remoteMessage.getData().size() > 0) {
            Log.d(TAG, "Message data payload: " + remoteMessage.getData());
            Map<String, String> mapData =remoteMessage.getData();

            if (mapData.containsKey("sendLocation")) {
                // For long-running tasks (10 seconds or more) use WorkManager.
                scheduleJob(mapData);
            } else {
                // Handle message within 10 seconds
                handleNow();
            }

        }
        RemoteMessage.Notification notification = remoteMessage.getNotification();
        // Check if message contains a notification payload.
        if (notification != null) {
            String body = notification.getBody();
            Log.d(TAG, "Message Notification Body: " + body);
            sendNotification(body);
        }

        // Also if you intend on generating your own notifications as a result of a received FCM
        // message, here is where that should be initiated. See sendNotification method below.
    }
    // [END receive_message]


    // [START on_new_token]

    /**
     * Called if InstanceID token is updated. This may occur if the security of
     * the previous token had been compromised. Note that this is called when the InstanceID token
     * is initially generated so this is where you would retrieve the token.
     */
    @Override
    public void onNewToken(String token) {
        Log.d(TAG, "Refreshed token: " + token);

        // If you want to send messages to this application instance or
        // manage this apps subscriptions on the server side, send the
        // Instance ID token to your app server.
        sendRegistrationToServer(token);
    }
    // [END on_new_token]

    /**
     * Schedule async work using WorkManager.
     */
    private void scheduleJob( Map<String, String> mapData) {
        // [START dispatch_job]
        OneTimeWorkRequest work = new OneTimeWorkRequest.Builder(MyWorker.class)
                .build();
        WorkManager.getInstance().beginWith(work).enqueue();
        // [END dispatch_job]
    }

    /**
     * Handle time allotted to BroadcastReceivers.
     */
    private void handleNow() {
        Log.d(TAG, "Short lived task is done.");
    }

    /**
     * Persist token to third-party servers.
     *
     * Modify this method to associate the user's FCM InstanceID token with any server-side account
     * maintained by your application.
     *
     * @param token The new token.
     */
    private void sendRegistrationToServer(String token) {
        getServiceProxy().requestAddDeviceNotificationTokenTask(token, new ResultCallback() {
            @Override
            public void complete(RequestResult requestResult) {
                if(requestResult.exception == null)
                    Log.e(TAG, requestResult.exception.toString());
            }
        });
    }

    /**
     * Create and show a simple notification containing the received FCM message.
     *
     * @param messageBody FCM message body received.
     */
    private void sendNotification( String messageBody) {
        Intent intent = new Intent(this, MapsActivity.class);
        intent.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP);
        PendingIntent pendingIntent = PendingIntent.getActivity(this, 0 /* Request code */, intent,
                PendingIntent.FLAG_ONE_SHOT);

        String channelId = getString(R.string.default_notification_channel_id);
        Uri defaultSoundUri = RingtoneManager.getDefaultUri(RingtoneManager.TYPE_NOTIFICATION);
        NotificationCompat.Builder notificationBuilder =
                new NotificationCompat.Builder(this, channelId)
                        .setSmallIcon(R.drawable.home_map_marker)
                        .setContentTitle(getString(R.string.default_notification_title))
                        .setContentText(messageBody)
                        .setAutoCancel(true)
                        .setSound(defaultSoundUri)
                        .setContentIntent(pendingIntent);

        NotificationManager notificationManager =
                (NotificationManager) getSystemService(Context.NOTIFICATION_SERVICE);

        // Since android Oreo notification channel is needed.
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            NotificationChannel channel = new NotificationChannel(channelId,
                    getString(R.string.default_notification_title),
                    NotificationManager.IMPORTANCE_DEFAULT);
            notificationManager.createNotificationChannel(channel);
        }

        notificationManager.notify(0 /* ID of notification */, notificationBuilder.build());
    }

    @Override
    public NetworkInfo getActiveNetworkInfo() {
        ConnectivityManager connectivityManager =
                (ConnectivityManager) getSystemService(Context.CONNECTIVITY_SERVICE);
        NetworkInfo networkInfo = connectivityManager.getActiveNetworkInfo();
        return networkInfo;
    }
}
