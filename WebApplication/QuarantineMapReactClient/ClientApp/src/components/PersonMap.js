import React, { Component } from 'react';
import {
    YMaps,
    Map,
    Circle
} from "react-yandex-maps";
import {Container} from "react-bootstrap";
import PropTypes from "prop-types";

export class PersonMap extends Component {
    static displayName = PersonMap.name;
    constructor(props) {
        super(props);
    }
    componentDidMount() {
        
    }
    
    render() {
        let zoneCicle =  this.props.Zone != null ?  (<Circle  key='zone'
                                  modules={['geoObject.addon.balloon', 'geoObject.addon.hint']}
                                  properties={{
                                      hintContent : 'Зона карантина ' +this.props.Name }}
                                  options={{
                                      preset: 'islands#violetIcon'
                                  }}
                                  geometry={[[this.props.Zone.lat, this.props.Zone.lon], this.props.Zone.radius]} />) : null;
        let centerMap = this.props.Zone;
        let zoom = 17;
        let locationCicle =  this.props.Location != null ?  (<Circle  key='location'
                                                              modules={['geoObject.addon.balloon', 'geoObject.addon.hint']}
                                                              properties={{
                                                                  hintContent : 'Текущее местоположение ' +this.props.Name }}
                                                              options={{
                                                                  preset: 'islands#violetIcon'
                                                              }}
                                                              geometry={[[this.props.Location.lat, this.props.Location.lon], this.props.Location.radius]} />) : null;
        if( this.props.Location != null )
            centerMap = this.props.Location;
        if(centerMap == null) {
            centerMap = {
                lat: 55.75,
                lon: 37.57,
                radius: 10
            }
            zoom = 9;
        }else{
            zoom = 17 - (centerMap.radius / 79000);
        }
        return (
            <div style={{marginTop: 10}}>
                <Container>
                    <YMaps>
                        <Map defaultState={{ center: [centerMap.lat, centerMap.lon], zoom: zoom}}
                             options={{ searchControlProvider: 'yandex#search'}}  height={470} width='100%' >
                            {zoneCicle}
                            {locationCicle}
                        </Map>
                    </YMaps>
                </Container>
            </div>
        );
    }

}

PersonMap.propTypes = {
    Location: PropTypes.any,
    Zone: PropTypes.any,
    Name: PropTypes.string
};