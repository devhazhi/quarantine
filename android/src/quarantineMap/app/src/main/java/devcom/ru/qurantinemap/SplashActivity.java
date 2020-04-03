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

import java.util.UUID;

import devcom.ru.qurantinemap.api.models.PersonObject;
import devcom.ru.qurantinemap.service.DownloadTask;
import devcom.ru.qurantinemap.service.DownloadCallback;
import devcom.ru.qurantinemap.service.ServiceProxy;

public class SplashActivity extends AppCompatActivity implements DownloadCallback<String> {

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_splash);

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
        String str =ServiceProxy.createDefault().getPersonByDeviceUrl();
        AsyncTask<String, Integer, DownloadTask.Result> execute = new DownloadTask((DownloadCallback<String>) this).execute(str);
    }

    @Override
    public void onRequestPermissionsResult(int requestCode,
                                           String[] permissions, int[] grantResults) {
        switch (requestCode) {
            case 1: {
                // If request is cancelled, the result arrays are empty.
                if (grantResults.length > 0
                        && grantResults[0] == PackageManager.PERMISSION_GRANTED) {
                    LoadData();
                } else {
                    finish();
                }
                return;
            }

            // other 'case' lines to check for other
            // permissions this app might request.
        }
    }

    @Override
    public void updateFromDownload(String person) {
        PersonObject po = ServiceProxy.createDefault().parsePersonObject(person);

        Intent intent = null;
        if (po == null) {
            intent = new Intent(SplashActivity.this, SignActivity.class);
        } else {
            intent = new Intent(SplashActivity.this, MapsActivity.class);
            intent.putExtra("dataPersonString", person);
        }
        startActivity(intent);
    }

    @Override
    public NetworkInfo getActiveNetworkInfo() {
        ConnectivityManager connectivityManager =
                (ConnectivityManager) getSystemService(Context.CONNECTIVITY_SERVICE);
        NetworkInfo networkInfo = connectivityManager.getActiveNetworkInfo();
        return networkInfo;
    }

    @Override
    public void onProgressUpdate(int progressCode, int percentComplete) {

    }

    @Override
    public void finishDownloading() {

    }
    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        finish();
    }
}
