package devcom.ru.qurantinemap.api.models;

import com.google.gson.Gson;

public class DeviceFile {
    public String deviceId;
    public String data;
    public int fileType;
    public String tryToJson(){
        try {
            return new Gson().toJson(this);
        }catch(Exception e){
            return null;
        }
    }
}
