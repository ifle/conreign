'use strict';
import _ from 'lodash';
import React from 'react';
import CSSModules from 'react-css-modules';

import {compose, resolve, connect} from 'core/core';
import {arrive} from './actions';
import styles from './game-menu.scss';

function init({dispatch, menu}) {
    if (menu) {
        return;
    }
    return dispatch(arrive());
}

export class GameMenuBase extends React.Component {
    componentWillMount() {
        init(this.props);
    }
    render() {
        return (
            <div styleName="game-menu-wrapper">
                <div styleName="game-menu">
                    <h1>Welcome to Conreign Universe,</h1>
                    <div styleName="menu-input">
                        <input type="text" value={this.props.player.name}/>
                    </div>
                    <h3>Your galaxy coordinates:</h3>
                    <div styleName="menu-input">
                        <input type="text"/>
                    </div>
                    <button>Navigate</button>
                </div>
            </div>
        );
    }
}

function mapStateToProps(state) {
    return state.menu || {};
}

const decorator = compose(
    connect(mapStateToProps),
    _.partial(CSSModules, _, styles),
    resolve(init)
);

export {menu} from './reducer';
export const GameMenu = decorator(GameMenuBase);
export default GameMenu;