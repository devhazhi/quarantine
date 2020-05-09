package devcom.ru.qurantinemap;

import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.net.ConnectivityManager;
import android.net.NetworkInfo;
import android.os.AsyncTask;
import android.os.Bundle;

import androidx.appcompat.app.AppCompatActivity;

import com.google.gson.Gson;

import java.util.UUID;

import devcom.ru.qurantinemap.api.models.PersonObject;
import devcom.ru.qurantinemap.service.DownloadTask;
import devcom.ru.qurantinemap.service.DownloadCallback;
import devcom.ru.qurantinemap.service.NetworkInfoCallback;
import devcom.ru.qurantinemap.service.RequestResult;
import devcom.ru.qurantinemap.service.RequestUrlTask;
import devcom.ru.qurantinemap.service.ResultCallback;
import devcom.ru.qurantinemap.service.ServiceProxy;

public class SplashActivity extends AppCompatActivity implements NetworkInfoCallback {

    private ServiceProxy serviceProxy;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_splash);
        serviceProxy = new ServiceProxy(this);
        LoadData();
    }

    private void LoadData() {

        SharedPreferences sharedPref = this.getPreferences(Context.MODE_PRIVATE);
        String appId =sharedPref.getString("app_id", "null");;
        if(appId =="null") {
            SharedPreferences.Editor editor = sharedPref.edit();
            appId = UUID.randomUUID().toString();
            editor.putString("app_id", appId);
            editor.commit();
        }
        ServiceProxy.deviceId =appId;
        serviceProxy.requestPersonByDeviceTask(new ResultCallback() {
            @Override
            public void complete(RequestResult requestResult) {
                if(requestResult == null) return;
                Intent intent = null;
                PersonObject res =PersonObject.tryFromJson(requestResult.resultValue);
                if (res == null) {
                    intent = new Intent(SplashActivity.this, SignActivity.class);
                } else {
                    intent = new Intent(SplashActivity.this, MapsActivity.class);
                    intent.putExtra("dataPersonString", requestResult.resultValue);
                }
                startActivity(intent);
            }

        });
    }

    @Override
    public NetworkInfo getActiveNetworkInfo() {
        ConnectivityManager connectivityManager =
                (ConnectivityManager) getSystemService(Context.CONNECTIVITY_SERVICE);
        NetworkInfo networkInfo = connectivityManager.getActiveNetworkInfo();
        return networkInfo;
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        finish();
    }
}
