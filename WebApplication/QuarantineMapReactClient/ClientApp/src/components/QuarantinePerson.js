import React, { Component } from 'react';
import {Container,  Alert} from "react-bootstrap";
import {Loading} from "./Loading";
import {PersonInfo} from "./PersonInfo";
import PropTypes from "prop-types";
import {PersonMap} from "./PersonMap";

export class QuarantinePerson extends Component {
    static displayName = QuarantinePerson.name;
    constructor(props) {
        super(props);
        this.state ={
            loading: true,
            error: "",
            sendLocationInfo:'',
            person: null,
            location: null
        };
        this.currentPosition = this.currentPosition.bind(this);
        this.addLocation = this.addLocation.bind(this);
        this.errorSendLocation = this.errorSendLocation.bind(this);
        this.successSendLocation = this.successSendLocation.bind(this);
    }
    componentDidMount() {
        this.loadInfo();
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(this.currentPosition);
        } else {
            alert("Нет доступа к геолокации");
        }
    }
    currentPosition(position) {
        let lat = position.coords.latitude;
        let long = position.coords.longitude;
        var positionCurrent = { lat: lat,
            lon: long,
            radius: position.coords.accuracy
        };
        console.log(positionCurrent);
        this.addLocation(position, this.successSendLocation, this.errorSendLocation)

        this.setState({
            location: positionCurrent
        });

    }
    render() {
        let errorInfo =  this.state.error && this.state.error.length > 0 ? (<p>{this.state.error}</p>) : null;
        let loadingState =  (this.state.loading ? <Loading /> : null);
        console.log(this.state.person);
        let personInfo =  this.state.person !== null ? (
            <PersonInfo Name={this.state.person.name}
                        QuarantineStopInfo={(new Date(this.state.person.quarantineStopUnix * 1000)).toLocaleDateString()}
                        SendLocationInfo={this.state.sendLocationInfo}/>
            )  : null;
        let personMap =  this.state.person !== null ? (
            <PersonMap Name={this.state.person.name}
                       Zone={this.state.person.zone}
                       Location={this.state.location}/>
        )  : null;
        return (
            <Container>
                {loadingState}
                {personInfo}
                {errorInfo}
                {personMap}             
            </Container>
        );
    }
    
    async loadInfo() {
        console.log('loadInfo')
        let person =null;
        let errorMessage = "";
        try {
            let res = window.localStorage.getItem('Identity');
            console.log(res);
            const response = await fetch('https://quarantinemap.ru/api/v1/Device/GetPersonByDevice?device_id=' + res);
            person = await response.json();

            console.log(person);
            //errorMessage='Без ошибок но не реализован просмотр информациий';
        }
         catch (error) {
            errorMessage = 'Ошибка загрузки зон карантина!';
            console.log(errorMessage, error)     
        }
        this.setState({
            loading: false,
            error: errorMessage,
            person: person
        }
            );
    }   
    
    successSendLocation(result) {
        this.setState({
                error: null,
                sendLocationInfo: result
            }
        );
    }
    errorSendLocation(result) {
        this.setState({
                error: result,
                sendLocationInfo: null
            }
        );
    }
    addLocation( position, onResult, onError) {
        // Default options are marked with *
        fetch('https://192.168.7.3:5001/api/v1/Device/SendLocation', {
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
                deviceId: window.localStorage.getItem('Identity'),
                lat: position.coords.latitude,
                lon: position.coords.longitude,
                radius: position.coords.accuracy
            }) // body data type must match "Content-Type" header
        }).then((response)=>response.json()).then((data) => {
            if (data.isOk) {
                onResult('Координаты успешно переданы');
                this.notifyMessage({
                    data: {
                        notifyType: "success"
                    },
                    notification: {
                        title: "Спасибо!",
                        body: ""
                    }
                });
             
            } else {
                onError('Нет связи');
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
                            body: "Передача невозможна"
                        }
                    });
                }
            }
            console.log(data); // JSON data parsed by `response.json()` call
        });
    }
    notifyMessage(notifyData) {
        console.log(notifyData);       
    }

}

