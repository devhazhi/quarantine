import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { Registry } from './components/Registry';
import { QuarantinePerson } from './components/QuarantinePerson';
import './custom.css'
import {QuarantineInfo} from "./components/QuarantineInfo";

export default class App extends Component {
  static displayName = App.name;

  render () {
    return (
      <Layout>
        <Route exact path='/' component={QuarantineInfo} />
        <Route exact path='/quarantinePerson' component={QuarantinePerson} />
        <Route path='/registry' component={Registry} />
      </Layout>
    );
  }
}
