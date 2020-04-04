package devcom.ru.qurantinemap.api.models;

import com.google.gson.Gson;

public class DeviceFile {
    public String deviceId;
    public byte[] data;
    public int fileType;
    public String tryToJson(){
        try {
            return new Gson().toJson(this);
        }catch(Exception e){
            return null;
        }
    }
}
