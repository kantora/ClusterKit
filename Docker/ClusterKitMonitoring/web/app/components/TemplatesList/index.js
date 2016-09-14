import React, { Component, PropTypes } from 'react';
import { Link } from 'react-router';

export default class TemplatesList extends Component { // eslint-disable-line react/prefer-stateless-function

  static propTypes = {
    templates: PropTypes.array.isRequired,
  }

  render() {
    const { templates } = this.props;


    return (
      <div>
        <h2>Templates list</h2>
        <Link to="/clusterkit/templates/create/" className="btn btn-primary" role="button">Add a new template</Link>
        <table className="table table-hover">
          <thead>
            <tr>
              <th>Code</th>
              <th>Name</th>
              <th>Packages</th>
              <th>Min</th>
              <th>Max</th>
              <th>Priority</th>
              <th>Version</th>
            </tr>
          </thead>
          <tbody>
          {templates && templates.length && templates.map((item) =>
            <tr key={item.Id}>
              <td>
                <Link to={`/clusterkit/templates/${item.Id}`}>
                  {item.Code}
                </Link>
              </td>
              <td>{item.Name}</td>
              <td>
                {item.Packages.map((pack) =>
                  <span key={`${item.Id}/${pack}`}>
                    <span className="label label-default">{pack}</span>{' '}
                  </span>
                )
                }
              </td>
              <td>{item.MinimumRequiredInstances}</td>
              <td>{item.MaximumNeededInstances}</td>
              <td>{item.Priority}</td>
              <td>{item.Version}</td>
            </tr>
          )
          }
          </tbody>
        </table>

      </div>
    );
  }
}

