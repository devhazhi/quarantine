package devcom.ru.qurantinemap;

import androidx.appcompat.app.AppCompatActivity;

import android.content.Context;
import android.content.Intent;
import android.graphics.Color;
import android.net.ConnectivityManager;
import android.net.NetworkInfo;
import android.os.AsyncTask;
import android.os.Bundle;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.TextView;

import devcom.ru.qurantinemap.api.models.PersonObject;
import devcom.ru.qurantinemap.api.models.Responce;
import devcom.ru.qurantinemap.service.DownloadTask;
import devcom.ru.qurantinemap.service.DownloadCallback;
import devcom.ru.qurantinemap.service.NetworkInfoCallback;
import devcom.ru.qurantinemap.service.RequestResult;
import devcom.ru.qurantinemap.service.ResultCallback;
import devcom.ru.qurantinemap.service.ServiceProxy;

public class SignActivity extends AppCompatActivity implements NetworkInfoCallback {

    private EditText edittext;
    private NetworkInfoCallback networkInfoCallback;
    private ServiceProxy serviceProxy;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_sigh);
        serviceProxy = new ServiceProxy(this);
        edittext = (EditText) findViewById(R.id.editText);
        Button btn = (Button) findViewById(R.id.button);
        networkInfoCallback = this;
        btn.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                if(edittext.length() == 11){
                    TextView text = (TextView) findViewById(R.id.errorText);
                    text.setText("");
                    serviceProxy.requestAddDevicePersonTask(Long.parseLong(edittext.getText().toString()), new ResultCallback() {
                        @Override
                        public void complete(RequestResult requestResult) {
                            completeAddDeviceResonce(requestResult);
                        }
                    });
                }
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



    public void completeGetPersonBy(RequestResult requestResult) {
        if(requestResult == null) {
            edittext.setText("Сеть не доступна");
            return;
        }
        PersonObject res = PersonObject.tryFromJson(requestResult.resultValue);
        if(res != null) {
            Intent intent = new Intent(SignActivity.this, MapsActivity.class);
            intent.putExtra("dataPersonString",  requestResult.resultValue);
            startActivity(intent);
        }
    }

    public void completeAddDeviceResonce(RequestResult requestResult) {
        if(requestResult == null) {
            edittext.setText("Сеть не доступна");
            return;
        }
        Responce res = Responce.tryFromJson(requestResult.resultValue);
        Boolean notNullIsOk =res != null && res.isOk != null;
        if( notNullIsOk && res.isOk) {
            serviceProxy.requestPersonByDeviceTask(new ResultCallback() {
                @Override
                public void complete(RequestResult requestResult) {
                    completeGetPersonBy(requestResult);
                }
            });
        }else {
            TextView text = (TextView) findViewById(R.id.errorText);
            if (notNullIsOk == false)
                text.setText(requestResult.resultValue);
            else if(res.error != null ) text.setText(res.error);
            else text.setText("Код ошибки " + res.errorCode);
            text.setTextColor(Color.rgb(255, 155, 155));
        }
    }
}
