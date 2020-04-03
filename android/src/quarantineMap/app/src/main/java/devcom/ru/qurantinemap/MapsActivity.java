package devcom.ru.qurantinemap;

import androidx.core.app.ActivityCompat;
import androidx.fragment.app.FragmentActivity;

import android.Manifest;
import android.content.Context;
import android.content.pm.PackageManager;
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
import android.widget.Toast;

import devcom.ru.qurantinemap.api.models.PersonObject;
import devcom.ru.qurantinemap.api.models.Responce;
import devcom.ru.qurantinemap.service.DownloadTask;
import devcom.ru.qurantinemap.service.DownloadCallback;
import devcom.ru.qurantinemap.service.ServiceProxy;

public class MapsActivity extends FragmentActivity implements OnMapReadyCallback, LocationListener,
        DownloadCallback<String> {

    private GoogleMap mMap;
    private Location lastCoord;
    private Circle _quarantineZona;
    private Location _quarantineLocation;
    private Marker _marker;
    protected LocationManager locationManager;
    private final int MY_PERMISSIONS_REQUEST_ACCESS_COARSE_LOCATION = 1;
    private final int MY_PERMISSIONS_REQUEST_ACCESS_FINE_LOCATION = 2;
    private AsyncTask<String, Integer, DownloadTask.Result> _lastExecute;
    private PersonObject _data;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_maps);
        try {
            String personString = getIntent().getStringExtra("dataPersonString");
            _data = ServiceProxy.createDefault().parsePersonObject(personString);
            Responce res = ServiceProxy.createDefault().getTryResponce(personString);
            if (res != null) {
                if (res.isOk != null && !res.isOk) {
                    _data = null;
                    ShowMessage(res.error, Toast.LENGTH_LONG);
                }

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
        if(_data != null && _data.zone != null) {
            _quarantineZona = mMap.addCircle(new CircleOptions().center(new LatLng(_data.zone.lat, _data.zone.lon)).radius(_data.zone.radius)
                    .fillColor(Color.argb(50, 255, 0, 0)));
        }
        Located();
    }

    private void Located() {
        if(mMap != null) {
            if (lastCoord != null) {
                if (_quarantineLocation == null && _data != null && _data.zone != null) {
                    _quarantineLocation = new Location(lastCoord);
                    _quarantineLocation.setLatitude(_data.zone.lat);
                    _quarantineLocation.setLongitude(_data.zone.lon);
                    _quarantineLocation.setAccuracy((int)_data.zone.radius);
                }
                String name ="Я";
                if(_data != null && _data.name != null)
                    name = _data.name;
                LatLng coord = new LatLng(lastCoord.getLatitude(), lastCoord.getLongitude());
                if (_marker == null) {
                    _marker = mMap.addMarker(new MarkerOptions().position(coord).title("Я"));
                } else _marker.setPosition(coord);
                if (_quarantineLocation!= null && _quarantineLocation.distanceTo(lastCoord) > _data.zone.radius) {
                    _marker.setTitle("вернитесь зону карантина!!!");
                } else _marker.setTitle(name);
                if(_data!=null && _data.quarantineStopUnix > 0) {
                    String url = ServiceProxy.createDefault().getAddLocationUrl(_marker.getPosition(), (int) lastCoord.getAccuracy());
                    _lastExecute = new DownloadTask(this)
                            .execute(url);
                }
                mMap.moveCamera(CameraUpdateFactory.newLatLng(coord));
            }
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
    public void updateFromDownload(String result) {
        Responce res = ServiceProxy.createDefault().getTryResponce(result);
        if(res != null && res.isOk) {
            ShowMessage("Координаты переданы", Toast.LENGTH_SHORT);
        }
        else {
            if(res ==null)
                ShowMessage("Ошибка передачи координат: " + result, Toast.LENGTH_LONG);
            else ShowMessage("Ошибка передачи координат: " + res.error, Toast.LENGTH_LONG);
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
        _lastExecute = null;
    }
}
