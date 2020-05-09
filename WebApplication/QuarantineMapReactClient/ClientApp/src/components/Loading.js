import React, { Component } from 'react';
import { Spinner } from "react-bootstrap";

export class Loading extends Component {
    static displayName = Loading.name;

  render() {
      return (          
          <Spinner animation="border" role="status">
                <span className="sr-only">Loading...</span>
          </Spinner>
      );;
  }
}
