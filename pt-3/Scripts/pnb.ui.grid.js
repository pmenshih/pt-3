;(function (window) {
    "use strict";

    function Grid(_properties) {
        var self = this;
        var props = function () {
            return {
                "divIdToDraw"                   : _properties["divIdToDraw"],
                "dataSet"                       : _properties["dataSet"],
                "columns"                       : _properties["columns"],
                "drawCompleteCallback"          : _properties["drawCompleteCallback"],
            };
        }();

        this.DrawComplete = function () {
            document.getElementById(props["divIdToDraw"]).innerHTML = "";
            self.DrawHeader();
            self.DrawBody();
        };

        this.DrawHeader = function () {
            var html = "<thead>";

            props["columns"].forEach(function(item, i, arr) {
                if (item.sortable) {
                    html += "<th id='" + props["divIdToDraw"] + 'col'
                        + item.sortable + "' class='uiGridSortableTh'>";
                }
                else html += "<th>";
                html += item.title;
                html += "</th>";
            });

            html += "</thead>";

            ChangeTagData("<thead>", html, true);
        };

        this.DrawBody = function () {
            var html = "<tbody>";

            props["dataSet"].forEach(function (rowItem, rowIdx, rowArr) {
                html += "<tr>";
                props["columns"].forEach(function (colItem, colIdx, colArr) {
                    var tpl = colItem.tpl;
                    if (tpl) {
                        if (tpl.indexOf('@') != -1) {
                            Object.keys(rowItem).forEach(function (item, i, arr) {
                                tpl = tpl.replace(new RegExp('@' + item + '@', 'g'), rowItem[item]);
                            });
                        }
                        tpl = tpl.replace(new RegExp('@@rowIdx@@', 'g'), rowIdx);
                        html += "<td>";
                        html += tpl;
                        html += "</td>";
                    }
                    else html += "<td></td>";
                });
                html += "</tr>";
            });

            html += "</tbody>";

            ChangeTagData("<tbody>", html, true);
        };

        this.properties = function (_properties) {
            if (!_properties) return props;
            props = _properties;
        };

        this.DeleteRow = function (idx) {
            props["dataSet"].splice(idx, 1);
            self.DrawBody();
        }

        function TableTagChecker(html) {
            if (html.slice(0, 7) === "<table>" && html.slice(-8) === "</table>") return html;
            return "<table>" + html + "</table>";
        }

        function ChangeTagData(tag, tagData, checkTableTag) {
            var html = document.getElementById(props["divIdToDraw"]).innerHTML;
            if (checkTableTag) html = TableTagChecker(html);

            if (html.indexOf(tag) === -1) {
                html = html.replace(/<table>/, "<table>" + tagData);
            }
            else {
                var regexp = new RegExp(tag + ".*" + tag.replace("<", "</"));
                html = html.replace(regexp, tagData);
            }

            HtmlSetter(html);
        }

        function HtmlSetter(html) {
            document.getElementById(props["divIdToDraw"]).innerHTML = html;

            document.querySelectorAll("#" + props["divIdToDraw"] + " th")
                .forEach(function (item, i, arr) {
                    item.addEventListener("click", function () {
                        var prop = (this).getAttribute('id').replace(new RegExp(props["divIdToDraw"] + 'col', 'g'), '');
                        var asc = (!(this).getAttribute('asc'));
                        $('#' + props["divIdToDraw"] + ' th').each(function () {
                            (this).removeAttribute('asc');
                        });
                        if (asc) (this).setAttribute('asc', 'asc');

                        props["dataSet"] = props["dataSet"].sort(function (a, b) {
                            if (asc) {
                                if (a[prop] > b[prop]) return 1;
                                if (a[prop] < b[prop]) return -1;
                                return 0;
                            } else {
                                if (b[prop] > a[prop]) return 1;
                                if (b[prop] < a[prop]) return -1;
                                return 0;
                            }
                        });

                        self.DrawBody();
                    });
                });

            typeof props["drawCompleteCallback"] === 'function' && props["drawCompleteCallback"]();
        }
    }

    window.PnB.UI.Grid = window.PnB.UI.Grid || Grid;

})(window);