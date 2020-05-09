import React, { Component, PureComponent, AbstractView} from 'react';
import {
    YMaps,
    Map,
    Clusterer,
    GeoObject,
    Placemark,
    ObjectManager,
    CommonProps,
    ClustererOptions
} from "react-yandex-maps";
import {Container} from "react-bootstrap";
import {Loading} from "./Loading";
import PropTypes from "prop-types";

export class LocationView extends React.PureComponent {
    static displayName = LocationView.name;
    constructor(props) {
        super(props);
        this.state ={
            loading: true,
            points: [],
            error: "",
            requestUrl:'',
            boundPoint: [[55, 37], [55.90, 37.90]]
        };
    }
    componentDidMount() {
        this.loadMarkers();
    }
    componentDidUpdate(prevProps, prevState, snapshot) {
        if(this.state.requestUrl != this.props.lastUrl){
           this.setState({ loading: true, requestUrl: this.props.lastUrl})   
           this.loadMarkers();
        }
    }

    render() {
        var points =  this.state.points ? this.state.points
                .map((point, index) => {
                    return <Placemark key={index}
                            modules={['geoObject.addon.balloon', 'geoObject.addon.hint']}
                            properties={{   
                                hintContent : 'Зона карантина ' +index,
                                clusterCaption: 'Зона карантина ' +index, }}
                            options={{
                                        preset: 'islands#violetIcon'
                                    }}
                            geometry={[point.lat, point.lon]} /> ;
                }) : null;
        var loadingState =  (this.state.loading ? <Loading /> : null);
       
        return (
                <Container>
                    {loadingState}
                    <YMaps>
                        <Map defaultState={{ center: [55.75, 37.57], zoom: 9}}
                             state={{bounds: this.state.boundPoint}}
                             options={{ searchControlProvider: 'yandex#search'}}  height={470} width='100%'  >
                            <Clusterer defaultOptions={{
                                preset: 'islands#invertedVioletClusterIcons',
                                groupByCoordinates: false,
                                clusterDisableClickZoom: false,
                                gridSize: 80,
                                clusterHideIconOnBalloonOpen: false,
                                geoObjectHideIconOnBalloonOpen: false
                            }}
                                       modules={['geoObject.addon.balloon', 'geoObject.addon.hint']}>
                                {points}
                            </Clusterer>
                        </Map>
                    </YMaps>
                </Container>
        );
    }
    
    async loadMarkers() {
        console.log('loadMaarkers')
        let points =null;
        let errorMessage = "";
        let boundPoint= [[55, 37], [55.90, 37.90]];
        try {
            var lastUrl = this.props.lastUrl;
            const response = await fetch('https://quarantinemap.ru/api/v1/Device/' + lastUrl);
            points = await response.json();

            points.map((point, index) => {
                if (point.lat < boundPoint[0][0])
                    boundPoint[0][0] = point.lat;
                else if (point.lat > boundPoint[1][0])
                    boundPoint[1][0] = point.lat;
                if (point.lon < boundPoint[0][1])
                    boundPoint[0][1] = point.lon;
                else if (point.lon > boundPoint[1][1])
                    boundPoint[1][1] = point.lon;
            });
         
        }
         catch (error) {
            errorMessage = 'Ошибка загрузки зон карантина!';
            console.log(errorMessage,error)     
        }
        this.setState({
            points: points,
            loading: false,
            error: errorMessage, 
            requestUrl: this.props.lastUrl,
            boundPoint: boundPoint}
            );
    }

}

LocationView.propTypes = {
    lastUrl: PropTypes.string
};