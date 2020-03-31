package sphaera.ru.qurantinemap;

import com.google.android.gms.maps.model.LatLng;
import com.google.gson.Gson;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.net.HttpURLConnection;
import java.net.MalformedURLException;
import java.net.URL;
import java.nio.charset.Charset;
import java.sql.Time;
import java.util.Date;
import java.util.Optional;
import java.util.stream.Collectors;

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
    public PersonObject getPersonByDevice() throws IOException {
        Optional<String> res = getRawResponse(getPersonByDeviceUrl());
        return parsePersonObject(res.get());
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

    public Responce AddDevicePerson(long phone) throws IOException {
        Optional<String> res = getRawResponse(getAddDevicePersonUrl(phone));
        return getTryResponce(res.get());
    }

    public String getAddDevicePersonUrl(long phone) {
        String item = _url +"AddDevicePerson?phone="+ phone +"&device_id=" +deviceId;
        return item;
    }

    public Responce getTryResponce(String res) {
        try {
            Gson gson = new Gson();
            return gson.fromJson(res, Responce.class);
        } catch (Exception e) {
            return null;
        }
    }

    public Responce AddLocation(LatLng latLng, int radius) throws IOException {
        Optional<String> res = getRawResponse(getAddLocationUrl(latLng, radius));
        return getTryResponce(res.get());
    }

    public String getAddLocationUrl(LatLng latLng, int radius) {
        return _url +"AddLocation?device_id=" +deviceId +"&lat="+ latLng.latitude+"&lon="+ latLng.longitude+"&radius="+ radius;
    }

    public Optional<String> getRawResponse(String url)
            throws MalformedURLException, IOException {
        HttpURLConnection connection = (HttpURLConnection) new URL(url).openConnection();
        try {
            connection.setConnectTimeout(10000);
            connection.setRequestMethod("GET");
            connection.setDoInput(true);
            Integer res =connection.getResponseCode();
            if (res != 200) {
                System.err.println("connection failed");
                return Optional.empty();
            }

            try (BufferedReader reader = new BufferedReader(
                    new InputStreamReader(connection.getInputStream(), Charset.forName("utf-8")))) {
                return Optional.of(reader.lines().collect(Collectors.joining(System.lineSeparator())));
            }
        }finally {
            connection.disconnect();
        }
    }

}


