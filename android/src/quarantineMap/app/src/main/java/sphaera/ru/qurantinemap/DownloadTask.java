package sphaera.ru.qurantinemap.data;

import android.net.ConnectivityManager;
import android.net.NetworkInfo;
import android.os.AsyncTask;

import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.Reader;
import java.io.UnsupportedEncodingException;
import java.net.HttpURLConnection;
import java.net.URL;

import javax.net.ssl.HostnameVerifier;
import javax.net.ssl.HttpsURLConnection;
import javax.net.ssl.SSLSession;

import sphaera.ru.qurantinemap.DownloadCallback;

/**
 * Implementation of AsyncTask designed to fetch data from the network.
 */
public class DownloadTask extends AsyncTask<String, Integer, DownloadTask.Result> {

    private DownloadCallback<String> callback;

    public DownloadTask(DownloadCallback<String> callback) {

        setCallback(callback);
    }

    void setCallback(DownloadCallback<String> callback) {
        this.callback = callback;
    }

    /**
     * Wrapper class that serves as a union of a result value and an exception. When the download
     * task has completed, either the result value or exception can be a non-null value.
     * This allows you to pass exceptions to the UI thread that were thrown during doInBackground().
     */
    public static class Result {
        public String resultValue;
        public Exception exception;
        public Result(String resultValue) {
            this.resultValue = resultValue;
        }
        public Result(Exception exception) {
            this.exception = exception;
        }
    }

    /**
     * Cancel background network operation if we do not have network connectivity.
     */
    @Override
    protected void onPreExecute() {
        if (callback != null) {
            NetworkInfo networkInfo = callback.getActiveNetworkInfo();
            if (networkInfo == null || !networkInfo.isConnected() ||
                    (networkInfo.getType() != ConnectivityManager.TYPE_WIFI
                            && networkInfo.getType() != ConnectivityManager.TYPE_MOBILE)) {
                // If no connectivity, cancel task and update Callback with null data.
                callback.updateFromDownload(null);
                cancel(true);
            }
        }
    }

    /**
     * Defines work to perform on the background thread.
     */
    @Override
    protected DownloadTask.Result doInBackground(String... urls) {
        Result result = null;
        if (!isCancelled() && urls != null && urls.length > 0) {
            String urlString = urls[0];
            try {
                URL url = new URL(urlString);
                String resultString = downloadUrl(url);
                if (resultString != null) {
                    result = new Result(resultString);
                } else {
                    throw new IOException("No response received.");
                }
            } catch(Exception e) {
                result = new Result(e);
            }
        }
        return result;
    }

    /**
     * Updates the DownloadCallback with the result.
     */
    @Override
    protected void onPostExecute(Result result) {
        if (result != null && callback != null) {
            if (result.exception != null) {
                callback.updateFromDownload(result.exception.getMessage());
            } else if (result.resultValue != null) {
                callback.updateFromDownload(result.resultValue);
            }
            callback.finishDownloading();
        }
    }

    /**
     * Override to add special behavior for cancelled AsyncTask.
     */
    @Override
    protected void onCancelled(Result result) {
    }

    /**
     * Given a URL, sets up a connection and gets the HTTP response body from the server.
     * If the network request is successful, it returns the response body in String form. Otherwise,
     * it will throw an IOException.
     */
    private String downloadUrl(URL url) throws IOException {
        InputStream stream = null;
        HttpURLConnection connection = null;
        String result = null;
        try {
            connection = (HttpURLConnection) url.openConnection();
            // Timeout for reading InputStream arbitrarily set to 3000ms.
            connection.setReadTimeout(10000);
            // Timeout for connection.connect() arbitrarily set to 3000ms.
            connection.setConnectTimeout(10000);
            // For this use case, set HTTP method to GET.
            connection.setRequestMethod("GET");
            // Already true by default but setting just in case; needs to be true since this request
            // is carrying an input (response) body.
            connection.setDoInput(true);
            // Open communications link (network traffic occurs here).
            connection.connect();
            publishProgress(DownloadCallback.Progress.CONNECT_SUCCESS);
            int responseCode = connection.getResponseCode();
            if (responseCode != HttpsURLConnection.HTTP_OK) {
                throw new IOException("HTTP error code: " + responseCode);
            }
            // Retrieve the response body as an InputStream.
            stream = connection.getInputStream();
            publishProgress(DownloadCallback.Progress.GET_INPUT_STREAM_SUCCESS, 0);
            if (stream != null) {
                // Converts Stream to String with max length of 500.
                result = readStream(stream, 500);
            }
        } finally {
            // Close Stream and disconnect HTTPS connection.
            if (stream != null) {
                stream.close();
            }
            if (connection != null) {
                connection.disconnect();
            }
        }
        return result;
    }

    /**
     * Converts the contents of an InputStream to a String.
     */
    public String readStream(InputStream stream, int maxReadSize)
            throws IOException, UnsupportedEncodingException {
        Reader reader = null;
        reader = new InputStreamReader(stream, "UTF-8");
        char[] rawBuffer = new char[maxReadSize];
        int readSize;
        StringBuffer buffer = new StringBuffer();
        while (((readSize = reader.read(rawBuffer)) != -1) && maxReadSize > 0) {
            if (readSize > maxReadSize) {
                readSize = maxReadSize;
            }
            buffer.append(rawBuffer, 0, readSize);
            maxReadSize -= readSize;
        }
        return buffer.toString();
    }
}
