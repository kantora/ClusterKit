/**
*
* NodesList
*
*/

import React, { Component, PropTypes } from 'react';

import styles from './styles.css';

export default class NodesList extends Component { // eslint-disable-line react/prefer-stateless-function

  static propTypes = {
    nodes: PropTypes.array.isRequired,
    onManualUpgrade: PropTypes.func.isRequired,
    hasError: PropTypes.bool.isRequired,
  }

  drawRole(node, role) {
    const isLeader = node.LeaderInRoles.indexOf(role) >= 0;
    return (<span key={`${node.NodeId}/${role}`}>
                {isLeader && <span className="label label-info" title={`${role} leader`}>{role}</span>}
                {!isLeader && <span className="label label-default">{role}</span>}
                {' '}
    </span>);
  }

  render() {
    const { nodes, onManualUpgrade, hasError } = this.props;


    return (
      <div className={styles.nodesList}>
        <h2>Nodes list</h2>
        {hasError &&
          <div className="alert alert-danger" role="alert">
            <span className="glyphicon glyphicon-exclamation-sign" aria-hidden="true"></span>
            <span> Could not connect to the server</span>
          </div>
        }
        <table className="table table-hover">
          <thead>
            <tr>
              <th>Address</th>
              <th>Leader</th>
              <th>Modules</th>
              <th>Roles</th>
              <th>Status</th>
              <th>Template</th>
              <th>Container</th>
            </tr>
          </thead>
          <tbody>
          {nodes && nodes.map((node) =>
            <tr key={`${node.NodeAddress.Host}:${node.NodeAddress.Port}`}>
              <td>{node.NodeAddress.Host}:{node.NodeAddress.Port}</td>
              <td>{node.IsClusterLeader ? <i className="fa fa-check-circle" aria-hidden="true"></i> : ''}</td>
              <td>
                {node.Modules.map((subModule) =>
                  <span key={`${node.NodeId}/${subModule.Id}`}>
                    <span className="label label-default">{subModule.Id}&nbsp;{subModule.Version}</span>{' '}
                  </span>
                )
                }
              </td>
              <td>
                {node.Roles.map((role) => this.drawRole(node, role))}
              </td>
              {node.IsInitialized &&
                <td>
                  <span className="label">{node.IsInitialized}</span>
                  {!node.IsObsolete &&
                    <span className="label label-success">Actual</span>
                  }
                  {node.IsObsolete &&
                    <span className="label label-warning">Obsolete</span>
                  }
                  <br />
                  <button
                    type="button" className={`${styles.upgrade} btn btn-xs`}
                    onClick={() => onManualUpgrade && onManualUpgrade(node)}
                  >
                    <i className="fa fa-refresh" /> {' '}
                    Upgrade Node
                  </button>
                </td>
              }
              {!node.IsInitialized &&
                <td>
                  <span className="label label-info">Uncontrolled</span>
                </td>
              }
              <td>
                {node.NodeTemplate}
              </td>
              <td>
                {node.ContainerType}
              </td>
            </tr>
          )
          }
          </tbody>
        </table>

      </div>
    );
  }
}

