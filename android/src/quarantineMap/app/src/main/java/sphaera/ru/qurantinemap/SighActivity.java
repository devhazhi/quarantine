package sphaera.ru.qurantinemap;

import androidx.appcompat.app.AppCompatActivity;

import android.content.Context;
import android.content.Intent;
import android.graphics.Color;
import android.net.ConnectivityManager;
import android.net.NetworkInfo;
import android.os.AsyncTask;
import android.os.Bundle;
import android.view.KeyEvent;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.TextView;

import sphaera.ru.qurantinemap.data.DownloadTask;

public class SighActivity extends AppCompatActivity implements DownloadCallback<String> {

    private EditText edittext;
    private DownloadCallback<String> callback;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_sigh);
        edittext = (EditText) findViewById(R.id.editText);
        Button btn = (Button) findViewById(R.id.button);
        callback = this;
        btn.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                if(edittext.length() == 11){
                    TextView text = (TextView) findViewById(R.id.errorText);
                    text.setText("");
                    AsyncTask<String, Integer, DownloadTask.Result> execute = new DownloadTask(callback)
                            .execute(ServiceProxy.createDefault().getAddDevicePersonUrl(Long.parseLong(edittext.getText().toString())));
                }
            }
        });
    }

    @Override
    public void updateFromDownload(String result) {
        Responce res = ServiceProxy.createDefault().getTryResponce(result);
        if(res != null && res.isOk != null && res.isOk) {
            AsyncTask<String, Integer, DownloadTask.Result> execute = new DownloadTask(callback)
                    .execute(ServiceProxy.createDefault().getPersonByDeviceUrl());
        }else{

            PersonObject po = ServiceProxy.createDefault().parsePersonObject(result);
            Intent intent = null;
            if ( po != null) {
                intent = new Intent(SighActivity.this, MapsActivity.class);
                intent.putExtra("dataPersonString", result);
                startActivity(intent);
            }
            else{
                if(res == null || res.isOk == null || !res.isOk ){
                    TextView text = (TextView) findViewById(R.id.errorText);
                    if(res == null)
                        text.setText(result);
                    else  text.setText(res.error);
                    text.setTextColor(Color.rgb(255, 155, 155));
                }
            }
        }
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
}
