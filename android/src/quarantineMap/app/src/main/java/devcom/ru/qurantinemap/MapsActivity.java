package devcom.ru.qurantinemap;

import androidx.core.app.ActivityCompat;
import androidx.fragment.app.FragmentActivity;

import android.Manifest;
import android.content.Context;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.graphics.Bitmap;
import android.graphics.Color;
import android.net.ConnectivityManager;
import android.net.NetworkInfo;
import android.os.AsyncTask;
import android.os.Bundle;

import com.google.android.gms.maps.CameraUpdateFactory;
import com.google.android.gms.maps.GoogleMap;
import com.google.android.gms.maps.OnMapReadyCallback;
import com.google.android.gms.maps.SupportMapFragment;
import com.google.android.gms.maps.model.Circle;
import com.google.android.gms.maps.model.CircleOptions;
import com.google.android.gms.maps.model.LatLng;
import com.google.android.gms.maps.model.Marker;
import com.google.android.gms.maps.model.MarkerOptions;

import android.location.Location;
import android.location.LocationListener;
import android.location.LocationManager;
import android.provider.MediaStore;
import android.view.View;
import android.widget.Button;
import android.widget.ImageButton;
import android.widget.TextView;
import android.widget.Toast;

import java.io.ByteArrayOutputStream;
import java.io.DataInputStream;
import java.io.DataOutputStream;
import java.util.Base64;

import devcom.ru.qurantinemap.api.models.DeviceFile;
import devcom.ru.qurantinemap.api.models.NotificationSubscribeResponce;
import devcom.ru.qurantinemap.api.models.PersonObject;
import devcom.ru.qurantinemap.api.models.Responce;
import devcom.ru.qurantinemap.notification.NotificationFirebaseMessagingService;
import devcom.ru.qurantinemap.service.DownloadTask;
import devcom.ru.qurantinemap.service.NetworkInfoCallback;
import devcom.ru.qurantinemap.service.RequestResult;
import devcom.ru.qurantinemap.service.ResultCallback;
import devcom.ru.qurantinemap.service.ServiceProxy;

public class MapsActivity extends FragmentActivity implements OnMapReadyCallback, LocationListener,
        NetworkInfoCallback {
    static final int REQUEST_IMAGE_CAPTURE = 1;
    private GoogleMap mMap;
    private Location lastCoord;
    private Circle _quarantineZona;
    private Location _quarantineLocation;
    private Marker _marker;
    protected LocationManager locationManager;
    private final int MY_PERMISSIONS_REQUEST_ACCESS_COARSE_LOCATION = 1;
    private final int MY_PERMISSIONS_REQUEST_ACCESS_FINE_LOCATION = 2;
    private AsyncTask<String, Integer, DownloadTask.Result> _lastExecute;
    private PersonObject _personInfo;
    private ServiceProxy serviceProxy;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_maps);
        ImageButton btnAddPhoto = (ImageButton) findViewById(R.id.btnAddPhoto);
        btnAddPhoto.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                Intent takePictureIntent = new Intent(MediaStore.ACTION_IMAGE_CAPTURE);
                if (takePictureIntent.resolveActivity(getPackageManager()) != null) {
                    startActivityForResult(takePictureIntent, REQUEST_IMAGE_CAPTURE);
                }
            }
        });
        serviceProxy = new ServiceProxy(this);
        try {
            String personString = getIntent().getStringExtra("dataPersonString");
            _personInfo = PersonObject.tryFromJson(personString);
            if(_personInfo != null){
                serviceProxy.requestGetSubscribeNotificationInfoTask(new ResultCallback() {
                    @Override
                    public void complete(RequestResult requestResult) {
                        if(requestResult == null) return;
                        NotificationSubscribeResponce res = NotificationSubscribeResponce.tryFromJson(requestResult.resultValue);
                        NotificationFirebaseMessagingService.syncSubscribeTopic(res.topics);
                    }
                });
            }
            // Obtain the SupportMapFragment and get notified when the map is ready to be used.
            SupportMapFragment mapFragment = (SupportMapFragment) getSupportFragmentManager()
                    .findFragmentById(R.id.map);
            mapFragment.getMapAsync(this);

            if (ActivityCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION)
                    != PackageManager.PERMISSION_GRANTED &&
                    ActivityCompat.checkSelfPermission(this, Manifest.permission.ACCESS_COARSE_LOCATION)
                            != PackageManager.PERMISSION_GRANTED) {
                if (ActivityCompat.shouldShowRequestPermissionRationale(this,
                        Manifest.permission.ACCESS_FINE_LOCATION)) {
                } else {
                    ActivityCompat.requestPermissions(this,
                            new String[]{Manifest.permission.ACCESS_FINE_LOCATION},
                            MY_PERMISSIONS_REQUEST_ACCESS_FINE_LOCATION);
                }

                if (ActivityCompat.shouldShowRequestPermissionRationale(this,
                        Manifest.permission.ACCESS_FINE_LOCATION)) {
                } else {
                    ActivityCompat.requestPermissions(this,
                            new String[]{Manifest.permission.ACCESS_COARSE_LOCATION},
                            MY_PERMISSIONS_REQUEST_ACCESS_COARSE_LOCATION);
                }
                return;
            }
            requestLocation();
        } catch (Exception e) {
            ShowMessage(e.toString(), Toast.LENGTH_LONG);
        }
    }

    private void requestLocation() {
        if (ActivityCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION) != PackageManager.PERMISSION_GRANTED && ActivityCompat.checkSelfPermission(this, Manifest.permission.ACCESS_COARSE_LOCATION) != PackageManager.PERMISSION_GRANTED) {
            // TODO: Consider calling
            //    ActivityCompat#requestPermissions
            // here to request the missing permissions, and then overriding
            //   public void onRequestPermissionsResult(int requestCode, String[] permissions,
            //                                          int[] grantResults)
            // to handle the case where the user grants the permission. See the documentation
            // for ActivityCompat#requestPermissions for more details.
            return;
        }
        locationManager = (LocationManager) getSystemService(Context.LOCATION_SERVICE);
        if(locationManager != null)
            locationManager.requestLocationUpdates(LocationManager.NETWORK_PROVIDER, 0, 0, this);
    }

    private void ShowMessage(String text, int duration ) {
        Context context = getApplicationContext();
        Toast toast = Toast.makeText(context, text, duration);
        toast.show();
    }


    @Override
    public void onMapReady(GoogleMap googleMap) {
        mMap = googleMap;
        mMap.clear();
        mMap.setMinZoomPreference(15);
        if(_personInfo != null && _personInfo.zone != null) {
            _quarantineZona = mMap.addCircle(new CircleOptions().center(new LatLng(_personInfo.zone.lat, _personInfo.zone.lon)).radius(_personInfo.zone.radius)
                    .fillColor(Color.argb(50, 255, 0, 0)));
        }
        Located();
    }

    private void Located() {
        if(mMap != null) {
            if (lastCoord != null) {
                initQuarantineZone();
                String name ="Я";
                if(_personInfo != null && _personInfo.name != null)
                    name = _personInfo.name;
                LatLng coord = new LatLng(lastCoord.getLatitude(), lastCoord.getLongitude());
                if (_marker == null) {
                    _marker = mMap.addMarker(new MarkerOptions().position(coord).title(name));
                } else _marker.setPosition(coord);
                setTitleMarker(name);
                sendLocation();
                mMap.moveCamera(CameraUpdateFactory.newLatLng(coord));
            }
        }
    }

    private void sendLocation() {
        if(_personInfo!=null && _personInfo.quarantineStopUnix > 0) {
            serviceProxy.requestAddLocationTask(_marker.getPosition(), (int) lastCoord.getAccuracy(), new ResultCallback() {
                @Override
                public void complete(RequestResult requestResult) {
                    if(requestResult == null ){
                        ShowMessage("Сеть не доступна", Toast.LENGTH_SHORT);
                        return;
                    }
                    Responce res =Responce.tryFromJson(requestResult.resultValue);
                    if(res != null && res.isOk) {
                        ShowMessage("Координаты переданы", Toast.LENGTH_SHORT);
                    }
                    else {
                        if(res ==null || res.error == null)
                            ShowMessage("Ошибка передачи координат", Toast.LENGTH_LONG);
                        else ShowMessage("Ошибка передачи координат: " + res.error, Toast.LENGTH_LONG);
                    }
                }
            });
        }
    }

    private void setTitleMarker(String name) {
        if (_quarantineLocation!= null && _quarantineLocation.distanceTo(lastCoord) > _personInfo.zone.radius) {
            _marker.setTitle("вернитесь зону карантина!!!");
        } else _marker.setTitle(name);
    }

    private void initQuarantineZone() {
        if (_quarantineLocation == null && _personInfo != null && _personInfo.zone != null) {
            _quarantineLocation = new Location(lastCoord);
            _quarantineLocation.setLatitude(_personInfo.zone.lat);
            _quarantineLocation.setLongitude(_personInfo.zone.lon);
            _quarantineLocation.setAccuracy((int)_personInfo.zone.radius);
        }
    }

    @Override
    public void onLocationChanged(Location location) {
        lastCoord = location;
        Located();
    }

    @Override
    public void onStatusChanged(String provider, int status, Bundle extras) {

    }

    @Override
    public void onProviderEnabled(String provider) {

    }

    @Override
    public void onProviderDisabled(String provider) {

    }
    @Override
    public void onRequestPermissionsResult(int requestCode,
                                           String[] permissions, int[] grantResults) {
        switch (requestCode) {
            case MY_PERMISSIONS_REQUEST_ACCESS_FINE_LOCATION:
            case MY_PERMISSIONS_REQUEST_ACCESS_COARSE_LOCATION: {
                // If request is cancelled, the result arrays are empty.
                if (grantResults.length > 0
                        && grantResults[0] == PackageManager.PERMISSION_GRANTED) {
                    requestLocation();
                } else {
                    // permission denied, boo! Disable the
                    // functionality that depends on this permission.
                }
                return;
            }

            // other 'case' lines to check for other
            // permissions this app might request.
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
    protected void onActivityResult(final int requestCode, int resultCode, Intent data) {
        if (requestCode == REQUEST_IMAGE_CAPTURE && resultCode == RESULT_OK) {
            Bundle extras = data.getExtras();
            byte[] dat =extras.getByteArray("data");
            Bitmap imageBitmap =  (Bitmap)extras.get("data");
            ByteArrayOutputStream outStream = new ByteArrayOutputStream();
            imageBitmap.compress(Bitmap.CompressFormat.JPEG, 50,outStream );
            // JPEG file type = 1
            serviceProxy.requestAddDeviceFileTask(
                    Base64.getEncoder().encodeToString(outStream.toByteArray()), 1,
                    new ResultCallback() {
                        @Override
                        public void complete(RequestResult requestResult) {
                            if(requestResult == null) return;
                            Responce res= Responce.tryFromJson(requestResult.resultValue);
                            if(res == null || res.isOk == null) {
                                String exceptionMessage = "";
                                if(requestResult.exception != null)
                                    exceptionMessage = requestResult.exception.getMessage();
                                ShowMessage("Ошибка передачи фото: " +exceptionMessage, Toast.LENGTH_LONG);
                            }
                            else if(!res.isOk) ShowMessage("Ошибка передачи файла: "
                                    + res.error, Toast.LENGTH_LONG);
                            else {
                                ShowMessage("Фото успешно передано", Toast.LENGTH_LONG);
                            }
                        }
                    });

        }
    }

}
