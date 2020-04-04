package devcom.ru.qurantinemap.service;

public interface ResultCallback<T> {
    void complete(RequestResult requestResult);
}
