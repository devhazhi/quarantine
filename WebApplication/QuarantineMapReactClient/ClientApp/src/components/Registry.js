import React, { Component } from 'react';
import { Loading } from './Loading';

export class Registry extends Component {
    static displayName = Registry.name;
    constructor(props) {
        super(props);
        this.state = {
            phone: ''
            , error: ''
            , loading: true
            , addQuarantine: false
            , checkIn: false
        };
        this.handleSubmit = this.handleSubmit.bind(this);
        this.handleSubmitOrAdd = this.handleSubmitOrAdd.bind(this);
        this.handlePhoneChange = this.handlePhoneChange.bind(this);
    }
    componentDidMount() {
        this.checkLocalDevice();
    }
    handleSubmit(event) {
        this.setState({ addQuarantine: false });
        this.registry();
    }
    handleSubmitOrAdd(event) {
        this.setState({ addQuarantine: true });
        this.registry();
    }
    handlePhoneChange(event) {
        this.setState({ phone: event.target.value });
    }
    renderRegistry() {
        return (
            <div className="input-group mt-4 mb-4">
                <input id="phoneNumber" type="number" name="Phone"
                    value={this.state.phone}
                    onChange={this.handlePhoneChange}
                       className="form-control"
                    placeholder="Введите свой номер телефона"
                />
                <div className="input-group-append">
                    <button className="btn btn-success" type="button" onClick={this.handleSubmit} >Войти</button>
                </div>
                <div className="col-sm-12">
                    <button className="btn btn-light" type="button" onClick={this.handleSubmitOrAdd}>Добавить или обновить карантин</button>
                </div>
            </div>
        );
    }
    renderError() {
        if (!this.state.loading && this.state.error && this.state.error.length > 0) {
            return (
                <div className="col-sm-12">
                    <strong className="text-danger">{this.state.error}</strong>
                </div>
            );
        }
        return <></>;
    }
    render() {
        let caption = this.state.loading
            ? "Проверка устройства"
            : "Регистрация устройства";

        let content = this.state.loading
            ? <Loading />
            : this.renderRegistry();
        if (this.state.checkIn) {
            caption = "Вы зарегистрированны";
        }

        return (                  
                      
        <div>
            <div className="row justify-content-center">
                <div className="col-12 col-md-8 col-lg-6 text-center">
                    <h1 id="captionId">{caption}</h1>
                    {content}
                    {this.renderError()}
                </div>
            </div>
        </div>

        );
    }
    registry() {
        fetch('https://quarantinemap.ru/api/v1/Device/AddDevicePerson', {
            method: 'POST', // *GET, POST, PUT, DELETE, etc.
            mode: 'cors', // no-cors, *cors, same-origin
            cache: 'no-cache', // *default, no-cache, reload, force-cache, only-if-cached
            credentials: 'same-origin', // include, *same-origin, omit
            headers: {
                'Content-Type': 'application/json'
                // 'Content-Type': 'application/x-www-form-urlencoded',
            },
            redirect: 'follow', // manual, *follow, error
            referrerPolicy: 'no-referrer', // no-referrer, *client
            body: JSON.stringify({
                phone: this.state.phone,
                deviceId: this.state.phone,
                addQuarantine: this.state.addQuarantine
            }) // body data type must match "Content-Type" header
        }).then((responce) => { return responce.json(); }).then((data) => {
                if (data && data.isOk) {
                    this.notifyMessage({
                        data: {
                            notifyType: "success"
                        },
                        notification: {
                            title: "Спасибо!",
                            body: ""
                        }
                    });
                    window.localStorage.setItem('Identity', 'test_react_app');
                    document.location.href = "";
                } else {
                    if (data.error) {
                        this.notifyMessage({
                            data: {
                                notifyType: "danger"
                            },
                            notification: {
                                title: "Ошибка!",
                                body: data.error
                            }
                        });
                    }
                    else {
                        this.notifyMessage({
                            data: {
                                notifyType: "danger"
                            },
                            notification: {
                                title: "Ошибка!",
                                body: "Регистрация невозможна"
                            }
                        });
                    }
                }
                console.log(data); // JSON data parsed by `response.json()` call
            }, (reject) => {
                this.notifyMessage({
                    data: {
                        notifyType: "danger"
                    },
                    notification: {
                        title: "Ошибка!",
                        body: "Регистрация временно невозможна"
                    }
                });
            });

    }
    notifyMessage(notifyData) {
        console.log(notifyData);
        if (notifyData.data.notifyType === "danger")
            this.setState({ error: notifyData.notification.title + ' ' + notifyData.notification.body });
        else this.setState({ checkIn: true });     
    }
    getPersonByDevice(device_id, onSuccess, onError) {
        fetch('/api/v1/Device/GetPersonByDevice' + device_id)
            .then((response) => {
                return response.json();
            })
            .then((res) => {
                console.log(res);
                onSuccess();
            }, (r) => { onError(); });
    }
    checkLocalDevice() {
        
        console.log('check deviceId from local sorage.');
        this.setState({ loading: false });
        var res = window.localStorage.getItem('Identity');
        if (res != null && res.length > 10) {
            window.localStorage.setItem('IdentityStart', '1');
            this.setState({ loading: true });
            this.getPersonByDevice(res, function () {
                document.location.reload(true);

            }, function () {                    
                    this.setState({ loading: false });
                    window.localStorage.setItem('Identity', '');

            });

        }
    }
}


