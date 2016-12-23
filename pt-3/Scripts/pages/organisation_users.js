(function (window) {    
    "use strict";

    function filterOne(event) {
        var object = event.target || event;
        var value = object.value.toLowerCase();
        var row = object.getAttribute('id');

        var filtered = document.querySelectorAll('.datatbl tbody tr');
        for (var i = filtered.length - 1; i >= 0; i--) {
            var td = filtered[i].getElementsByClassName(row)[0];
            var display = filtered[i].style.display || "table-row";
            if (display === "table-row") {
                var text = td.innerText.toLowerCase();
                if (text.indexOf(value) === -1) filtered[i].style.display = "none";
                else filtered[i].style.display = "table-row";
            }
        }
    }

    function filterTwo(event) {
        var object = event.target || event;
        var state = object.checked;
        var row = object.value;
        var filtered = document.querySelectorAll('.datatbl tbody tr');
        for (var i = filtered.length - 1; i >= 0; i--) {
            if (filtered[i].getAttribute('data-rowtype') === row) {
                var display = filtered[i].style.display || "table-row";
                if (display === "table-row") {
                    if (state) filtered[i].style.display = "table-row";
                    else filtered[i].style.display = "none";
                }
            }
        }
    }

    function resetAll() {
        var filtered = document.querySelectorAll('.datatbl tbody tr');
        for (var i = filtered.length - 1; i >= 0; i--) {
            filtered[i].style.display = "table-row";
        }
    }

    function filterAll() {

        resetAll();

        var inputs = document.getElementsByTagName('input');
        
        for (var i = inputs.length - 1; i >= 0; i--) {
            var type = inputs[i].getAttribute('type') || 'text';
            //var type = inputs[i].getAttribute('type'); 
            if (type === 'checkbox') filterTwo(inputs[i]);
            if (type === 'text') filterOne(inputs[i]);
        }
    }

    function multifilter() {
        var inputs = document.getElementsByTagName('input');
        for (var i = inputs.length - 1; i >= 0; i--) {
            var type = inputs[i].getAttribute('type') || 'text';
            //var type = inputs[i].getAttribute('type');            
            if (type === 'checkbox') inputs[i].addEventListener('change', filterAll);
            if (type === 'text') inputs[i].addEventListener('keyup', filterAll);
        }
    }

    multifilter();
    
})(window);


   
