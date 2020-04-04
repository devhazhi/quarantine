package devcom.ru.qurantinemap.api.models;

import com.google.gson.Gson;

public class Responce
{
    public Boolean isOk;
    public String error;
    public int errorCode;
    public static Responce tryFromJson(String val){
        try {
            return new Gson().fromJson(val, Responce.class);
        }catch(Exception e){
            return null;
        }
    }
}
