class Grid {
    constructor(_divId, _data, _columns) {
        this.jsonData = _data;
        this.divId = _divId;
        this.divIdJQ = '#' + this.divId;
        this.columns = _columns;
    }

    Init() {
        if (this.jsonData.length < 1) {
            $(this.divIdJQ).empty();
            return;
        }

        let html = '<table>';

        html += "<tr>";
        $.each(this.columns, function (idx, obj) {
            html += "<th>";
            html += obj.title;
            html += "</th>";
        });
        html += "</tr>";

        var columns = this.columns;
        var data = this.jsonData;

        $.each(data, function (rowIdx, rowObj) {
            html += "<tr>";
            $.each(columns, function (colIdx, colObj) {
                var tpl = colObj.tpl;
                if (tpl) {
                    if (tpl.indexOf('@') != -1) {
                        $.each(Object.keys(rowObj), function (tIdx, tObj) {
                            tpl = tpl.replace(new RegExp('@' + tObj + '@', 'g'), rowObj[tObj]);
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

        html += "</table>";
        $(this.divIdJQ).html(html);
    }
};