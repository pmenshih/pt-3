(function (window) {
    var multifilters = (function(options){
       "use strict";


     });
})(window);





$('.checkcontainer').on('change', 'input[type="checkbox"]', function () {
    var cbs = $('.checkcontainer').find('input[type="checkbox"]');
    var all_checked_types = [];
    $('.filter').each(function () {
        Filter1($(this));
    });
    $.each(cbs, function (idx, obj) { 
        Filter2(obj);
    });

    /*
    Filter2($(this));
    */
});

/*
function Filter3() {
    var cbs = $('.checkcontainer');
    $.each(cbs, function (idx, obj) { 
        Filter2(obj);
    });
}
*/

function Filter2(obj) {
    var mycheckbox = obj;    
    var ckbox = mycheckbox.value;
    var rowtype = 'tr[data-rowtype="' + ckbox + '"]';
    var state = mycheckbox.checked;
    var filtered = $('.datatbl tbody tr:visible').filter(function () {
        if ($(this).data('rowtype') == mycheckbox.value)
            return true;
        else return false;  
    }).toggle(mycheckbox.checked);
}

$.fn.multifilter = function (options) {    
    var settings = $.extend({
        'target': $('table'),
        'method': 'thead' // This can be thead or class
    }, options);

    jQuery.expr[":"].Contains = function (a, i, m) {
        return (a.textContent || a.innerText || "").toUpperCase().indexOf(m[3].toUpperCase()) >= 0;
    };

    this.each(function () {
        var $this = $(this);
        var container = settings.target;
        var row_tag = 'tr';
        var item_tag = 'td';
        var rows = container.find($(row_tag));

        if (settings.method === 'thead') {  
            var col = container.find('th:Contains(' + $this.data('col') + ')');
            var col_index = container.find($('thead th')).index(col);
        };

        if (settings.method === 'class') {         
            var col = rows.first().find('td.' + $this.data('col') + '');
            var col_index = rows.first().find('td').index(col);
        };

        $this.change(function () {
            Filter1($(this));

            $.each($('input[type="checkbox"]'), function (idx, val) {
                Filter2(val);
            });
        }).keyup(function () { $this.change(); });
    });
}
  
function Filter1(obj, rows) {
    var filter = obj.val();
    var rows = $('table').find($("tr"));
    var item_tag = 'td';
    var col = $('table').find('th:Contains(' + obj.data('col') + ')');
    var col_index = $('table').find($('thead th')).index(col);
    rows.each(function () {
        var row = $(this);
        var cell = $(row.children(item_tag)[col_index]);

        if (filter) {
            if (cell.text().toLowerCase().indexOf(filter.toLowerCase()) !== -1) {
                cell.attr('data-filtered', 'positive');
            } else {
                cell.attr('data-filtered', 'negative');
            }
            if (row.find(item_tag + "[data-filtered=negative]").size() > 0) {
                row.hide();
            } else {
                if (row.find(item_tag + "[data-filtered=positive]").size() > 0) {
                    row.show();
                }
            }
        } else {
            cell.attr('data-filtered', 'positive');
            if (row.find(item_tag + "[data-filtered=negative]").size() > 0) {
                row.hide();
            } else {
                if (row.find(item_tag + "[data-filtered=positive]").size() > 0) {
                    row.show();
                }
            }
        }
    });
    return false;
}

$('.filter').multifilter();