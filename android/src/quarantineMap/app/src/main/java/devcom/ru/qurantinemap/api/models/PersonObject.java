package devcom.ru.qurantinemap.api.models;

import com.google.gson.Gson;

public class PersonObject
{
    public Location zone;
    public String name;
    public long quarantineStopUnix;
    public String token;
    public static PersonObject tryFromJson(String val){
        try {
            return new Gson().fromJson(val, PersonObject.class);
        }catch(Exception e){
            return null;
        }
    }
}
