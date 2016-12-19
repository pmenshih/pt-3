
$.fn.Filter = function () {
   
    var filterTarget = $(this);
    var child;
    if ($(this).is('table')) {
        child = 'tbody tr';
    }
     
    var hide;
    var show;
    var filter;
        
    document.getElementById('')
    
    getElementByTagPointClass('input.filter').addEventListener('keyup', DoFiltering);
    /*
    $('input.filter').keyup(function () {
        
        
        filter = $(this).val();
  
        hide = $(filterTarget).find(child + ':not(:Contains("' + filter + '"))');
        show = $(filterTarget).find(child + ':Contains("' + filter + '")');
        hide.slideUp(500);
        show.slideDown(500);
            

    });*/
        
    function DoFiltering() {
        filter = $(this).val();

        hide = $(filterTarget).find(child + ':not(:Contains("' + filter + '"))');
        show = $(filterTarget).find(child + ':Contains("' + filter + '")');
        hide.slideUp(500);
        show.slideDown(500);
    }

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

$('table').Filter();


