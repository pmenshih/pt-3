
      $.fn.Filter = function () {
   
        var filterTarget = $(this);
        var child;
        if ($(this).is('table')) {
            child = 'tbody tr';
        }
     
        var hide;
        var show;
        var filter;
  
        $('input.filter').keyup(function () {
           
            filter = $(this).val();
  
            hide = $(filterTarget).find(child + ':not(:Contains("' + filter + '"))');
            show = $(filterTarget).find(child + ':Contains("' + filter + '")');
            hide.slideUp(500);
            show.slideDown(500);
            

        });
        
        jQuery.expr[':'].Contains = function (a, i, m) {
            return jQuery(a).text().toLowerCase().indexOf(m[3].toLowerCase()) >= 0;
        };
    }

    $('table').Filter();


