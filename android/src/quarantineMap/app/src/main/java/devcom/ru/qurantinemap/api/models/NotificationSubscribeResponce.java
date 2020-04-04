package devcom.ru.qurantinemap.api.models;

import com.google.gson.Gson;

public class NotificationSubscribeResponce extends Responce {
    public String[] topics;
    public static NotificationSubscribeResponce tryFromJson(String val){
        try {
            return new Gson().fromJson(val, NotificationSubscribeResponce.class);
        }catch(Exception e){
            return null;
        }
    }
}
