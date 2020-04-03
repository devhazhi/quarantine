package devcom.ru.qurantinemap.service;

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

import devcom.ru.qurantinemap.api.models.PersonObject;
import devcom.ru.qurantinemap.api.models.Responce;

public class ServiceProxy {
    private final String _url;
    public static String deviceId;
    public static PersonObject personObject;

    public ServiceProxy(String url){
        _url = url;
    }

    public static ServiceProxy createDefault(){
        String host="dmhazhi-001-site1.dtempurl.com";
        //host="192.168.1.3:5000";
        return  new ServiceProxy("http://"+host+"/api/v1/Device/" );
    }
    public String getPersonByDeviceUrl()   {
        return _url +"GetPersonByDevice?device_id=" +deviceId;
    }
    public String getAddFileByDeviceUrl()   {
        return _url +"AddFileByDevice";
    }




    public PersonObject parsePersonObject(String res) {
        try {
            Gson gson = new Gson();
            personObject = gson.fromJson(res, PersonObject.class);
            return personObject;
        }catch (Exception e){
            return null;
        }
    }

    public String getAddDevicePersonUrl(long phone) {
        String item = _url +"AddDevicePerson?phone="+ phone +"&device_id=" +deviceId;
        return item;
    }

    public Responce parseResponce(String res) {
        try {
            Gson gson = new Gson();
            return gson.fromJson(res, Responce.class);
        } catch (Exception e) {
            return null;
        }
    }

    public String getAddLocationUrl(LatLng latLng, int radius) {
        return _url +"AddLocation?device_id=" +deviceId +"&lat="+ latLng.latitude+"&lon="+ latLng.longitude+"&radius="+ radius;
    }
    public String getAddDeviceNotificationTokenUrl(String token) {
        String item = _url +"AddDevicePerson?device_id="+ deviceId +"&token=" +token;
        return item;
    }

}


