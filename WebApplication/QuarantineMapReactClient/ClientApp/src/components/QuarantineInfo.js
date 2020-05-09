import React, { Component } from 'react';
import {Container, Dropdown, ButtonGroup, Button, Row} from "react-bootstrap";
import {LocationView} from "./LocationView";

export class QuarantineInfo extends Component {
    static displayName = QuarantineInfo.name;
    constructor(props) {
        super(props);
        this.state ={
            selectedView: 'zone',
        };
        this.handleZoneClick = this.handleZoneClick.bind(this);
        this.handleLastLocationClick = this.handleLastLocationClick.bind(this);
        this.handlePersonLocationClick = this.handlePersonLocationClick.bind(this);
    }
    handleZoneClick(eventKey) {
        this.setState({ selectedView: 'zone' });
    }
    handleLastLocationClick(eventKey) {
        this.setState({ selectedView: 'lastlocation' });
    }
    handlePersonLocationClick(eventKey) {
        this.setState({ selectedView: 'person_location' });
    }
    render() {
        let res = window.localStorage.getItem('Identity');
        let selectedCpation = null;
        let content = null;
        switch (this.state.selectedView) {
            case "zone":
                selectedCpation = 'Метки зон каратниа';
                content = (<LocationView lastUrl={'GetZonaLocations'}  />);
                break;
            case "lastlocation":
                selectedCpation = 'Метки местополложений';
                content = (<LocationView lastUrl={'GetPersonsLastLocations'}  />);
                break;
            case "person_location":
            
                selectedCpation = 'Переданные координаты местополложений';
                content = (<LocationView lastUrl={'GetPersonLocations?device_id=' + res}  />);
                break;
        }
        let personLocationButton =res && res.length > 5? (  <Button variant={this.state.selectedView === 'person_location' ? "success": "secondary"}
                                                                     className='btnQuarantineLocation'
                                                                     onClick={this.handlePersonLocationClick}>Хронология</Button>) :null;
        return (
            <Container>
                <Row >
                    <Button variant={this.state.selectedView === 'zone' ? "success": "secondary"} 
                            className='btnQuarantineLocation'
                            onClick={this.handleZoneClick}>Метки зон каратниа</Button>
                    <Button variant={this.state.selectedView === 'lastlocation' ? "success": "secondary"}
                            className='btnQuarantineLocation'
                            onClick={this.handleLastLocationClick}>Метки местополложений</Button>
                    {personLocationButton}
                </Row>
                {content}
            </Container>
        );
    }

}
