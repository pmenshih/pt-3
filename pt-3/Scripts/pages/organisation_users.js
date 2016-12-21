//(function ($) {
(function (window) {
    var multifilters = (function(options){
       "use strict";


     });
})(window);
    
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
            var filter = $this.val();
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
        }).keyup(function () {
            $this.change();
        });
    });
}
  
$('.filter').multifilter();




//})(jQuery);

/*



$.fn.Filter = function () {
   
    var filterTarget = $(this);
    var child;
    if ($(this).is('table')) {
        child = 'tbody > tr';
    }
     
    var hide;
    var show;
    var filter;


    
        
    document.getElementById('')   

    var filterSelector = document.querySelectorAll('.filter');
    for (var i = 0; i < filterSelector.length; i++) {
        filterSelector[i].addEventListener('keyup', DoFiltering);
    }   
        
    function DoFiltering() {
        filter = $(this).val();
        hide = $(filterTarget).find(child + ':not(:Contains("' + filter + '"))');
        show = $(filterTarget).find(child + ':Contains("' + filter + '")');
        hide.slideUp(500);
        show.slideDown(500);
    }

    var $rows = $('tbody > tr'),
    $filters = $('#filter_table input');

    var $i = $filters.filter(function () {
        return $.trim(this.value).length > 0;
    })

    var cls = '.' +$i.map(function () {
        return this.className
        }).get().join(',.');


    jQuery.expr[':'].Contains = function (a, i, m) {
        return jQuery(a).text().toLowerCase().indexOf(m[3].toLowerCase()) >= 0;
    };
}



function getElementByTagPointClass(selector)
{
    var tagName = selector.split('.')[0];
    var className = selector.split('.')[1];
    var elems = document.getElementsByTagName(tagName);
    for (var i = 0; i < elems.length; ++i) {
        var classesArr = elems[i].className.split(/\s+/);
        for (var j = 0; j < classesArr.length; ++j) {
            if (classesArr[j] == className) return elems[i];
        }
    }
}
*/
//$('table').Filter();

