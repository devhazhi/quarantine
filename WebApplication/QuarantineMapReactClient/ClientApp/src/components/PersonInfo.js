import React, { Component } from 'react';
import PropTypes from "prop-types";

export class PersonInfo extends Component {
    static displayName = PersonInfo.name;

    constructor(props) {
        super(props);
    }
    


    render() {        
        return (
            <div className="col-12 col-sm-12 m-auto ml-md-12 col-md-12 col-lg-12">
                <div className="fdb-box">
                    <div className="row justify-content-center">
                        <div className="col-12 col-xl-12 text-left">
                            <h1>{this.props.Name}</h1>

                            <dt className="col-sm-12">
                                <strong>Дата окончания</strong>
                            </dt>
                            <dd className="col-sm-12">
                                <strong className="text-danger">{this.props.QuarantineStopInfo}</strong>
                            </dd>
                            <dt className="col-sm-12">
                                <strong>Состояние передачи координат</strong>
                            </dt>
                            <dd className="col-sm-12">
                                <strong id="addLocationResult"
                                        className="text-success">{this.props.SendLocationInfo}</strong>
                            </dd>
                        </div>
                    </div>
                </div>
            </div>
        );
    } 
}

PersonInfo.propTypes = {
    QuarantineStopInfo: PropTypes.string,
    Name: PropTypes.string,
    SendLocationInfo: PropTypes.string
};
