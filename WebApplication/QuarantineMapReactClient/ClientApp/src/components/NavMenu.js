import React, { Component } from 'react';
import { Collapse, Container, Navbar, NavbarBrand, NavbarToggler, NavItem, NavLink } from 'reactstrap';
import { Link } from 'react-router-dom';
import './NavMenu.css';

export class NavMenu extends Component {
  static displayName = NavMenu.name;

  constructor (props) {
    super(props);

    this.toggleNavbar = this.toggleNavbar.bind(this);
    this.state = {
      collapsed: true
    };
  }

  toggleNavbar () {
    this.setState({
      collapsed: !this.state.collapsed
    });
  }

  render () {
    let res = window.localStorage.getItem('Identity');
    let quarantineLink = res && res.length > 0 ? (  
        <NavItem>
          <NavLink tag={Link} className="text-dark" to="/quarantinePerson">Карантин</NavLink>
        </NavItem>) : 
        (<NavItem>
          <NavLink tag={Link} className="text-dark" to="/registry">Регистрация</NavLink>
        </NavItem>);
    return (
      <header>
        <Navbar className="navbar-expand-sm navbar-toggleable-sm ng-white border-bottom box-shadow mb-3" light>
          <Container>
                    <NavbarBrand tag={Link} to="/"><img src="/images/quarantinemap.png" height="30" alt="image" /></NavbarBrand>
            <NavbarToggler onClick={this.toggleNavbar} className="mr-2" />
            <Collapse className="d-sm-inline-flex flex-sm-row-reverse" isOpen={!this.state.collapsed} navbar>
              <ul className="navbar-nav flex-grow">          
                {quarantineLink}
              </ul>
            </Collapse>
          </Container>
        </Navbar>
      </header>
    );
  }
}
