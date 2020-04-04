package devcom.ru.qurantinemap.service;

import android.app.Person;
import android.os.AsyncTask;

import androidx.annotation.NonNull;

import com.google.android.gms.maps.model.LatLng;
import com.google.gson.Gson;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.net.HttpURLConnection;
import java.net.MalformedURLException;
import java.net.URL;
import java.nio.charset.Charset;
import java.util.Optional;
import java.util.stream.Collectors;

import devcom.ru.qurantinemap.api.models.DeviceFile;
import devcom.ru.qurantinemap.api.models.PersonObject;
import devcom.ru.qurantinemap.api.models.Responce;

public class ServiceProxy {
    private final NetworkInfoCallback networkInfoCallback;
    private String _url;
    public static String deviceId;
    public static PersonObject personObject;

    private ServiceProxy(){
        initDefaultUrl();
        networkInfoCallback =null;
    }

    public ServiceProxy(NetworkInfoCallback callback ){
        initDefaultUrl();
        networkInfoCallback =callback;
    }

    private void initDefaultUrl() {
        String host="dmhazhi-001-site1.dtempurl.com";
        //host="192.168.1.3:5000";
        _url = "http://"+host+"/api/v1/Device/";
    }

    public static ServiceProxy createDefault(){
        return  new ServiceProxy();
    }

    public AsyncTask<String, Integer, RequestResult> requestPersonByDeviceTask(ResultCallback personObjectResultCallback)   {
        return new RequestUrlTask(networkInfoCallback, personObjectResultCallback, null)
                .execute(_url +"GetPersonByDevice?device_id=" +deviceId);
    }

    public AsyncTask<String, Integer, RequestResult> requestGetSubscribeNotificationInfoTask(ResultCallback personObjectResultCallback)   {
        return new RequestUrlTask(networkInfoCallback, personObjectResultCallback, null)
                .execute(_url +"GetSubscribeNotificationInfo?device_id=" +deviceId);
    }


    public AsyncTask<String, Integer, RequestResult> requestAddDeviceFileTask(@NonNull DeviceFile deviceFile,
                                                                                ResultCallback personObjectResultCallback) {
        return new RequestUrlTask(networkInfoCallback, personObjectResultCallback, new Gson().toJson(deviceFile))
                .execute(_url +"AddFileByDevice");
    }
    public AsyncTask<String, Integer, RequestResult> requestAddDevicePersonTask(long phone,
                                                                                              ResultCallback<Responce> personObjectResultCallback) {
        return new RequestUrlTask(networkInfoCallback, personObjectResultCallback, null)
                .execute(_url +"AddDevicePerson?phone="+ phone +"&device_id=" +deviceId);
    }

    public AsyncTask<String, Integer, RequestResult> requestAddLocationTask(LatLng latLng,
                                                                                          int radius,
                                                                                          ResultCallback<Responce> personObjectResultCallback)   {
        return new RequestUrlTask(networkInfoCallback, personObjectResultCallback, null)
                .execute(_url +"AddLocation?device_id=" +deviceId +"&lat="+ latLng.latitude+"&lon="+ latLng.longitude+"&radius="+ radius);
    }
    public AsyncTask<String, Integer, RequestResult> requestAddDeviceNotificationTokenTask(String token,
                                                                                                         ResultCallback<Responce> personObjectResultCallback)   {
        return new RequestUrlTask(networkInfoCallback, personObjectResultCallback, null)
                .execute(_url +"AddDeviceNotificationToken?device_id="+ deviceId +"&token=" +token);
    }
}


