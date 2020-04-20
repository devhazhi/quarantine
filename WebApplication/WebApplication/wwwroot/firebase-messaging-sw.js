console.log('[firebase-messaging-sw.js] start');
// If you do not serve/host your project using Firebase Hosting see https://firebase.google.com/docs/web/setup
importScripts('https://www.gstatic.com/firebasejs/7.14.1/firebase-app.js');
importScripts('https://www.gstatic.com/firebasejs/7.14.1/firebase-messaging.js');
importScripts('firebase_init.js');
console.log('[firebase-messaging-sw.js] 1');
const messaging = firebase.messaging();
console.log('[firebase-messaging-sw.js] 2');
messaging.setBackgroundMessageHandler(function (payload) {
    console.log('[firebase-messaging-sw.js] Received background message ', payload);
    // Customize notification here
    const notificationTitle = 'Background Message Title';
    const notificationOptions = {
        body: 'Background Message body.',
        icon: '/favicon.ico'
    };

    return self.registration.showNotification(notificationTitle,
        notificationOptions);
});