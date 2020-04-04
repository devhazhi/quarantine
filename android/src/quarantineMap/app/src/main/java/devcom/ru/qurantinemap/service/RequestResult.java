package devcom.ru.qurantinemap.service;

public class RequestResult {
    public String resultValue;
    public Exception exception;
    public RequestResult(String resultValue) {
        this.resultValue = resultValue;
    }
    public RequestResult(Exception exception) {
        this.exception = exception;
    }
}