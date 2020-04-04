package devcom.ru.qurantinemap.service;

import android.net.NetworkInfo;

public interface NetworkInfoCallback {
    /**
     * Get the device's active network status in the form of a NetworkInfo object.
     */
    NetworkInfo getActiveNetworkInfo();
}
