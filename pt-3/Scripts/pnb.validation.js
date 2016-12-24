; (function (window) {
    "use strict";

    function Validation(_properties) {
        var self = this;
        var props = function () {
            if (!_properties) _properties = true;
            return {
                "cssError": _properties["cssError"] || "inputValidateError",
            };
        }();

        this.properties = function (_properties) {
            if (!_properties) return props;
            props = _properties;
        };

        this.ClearInput = function (obj) {
            var descrDiv = document.getElementById(obj.id + "Validate");

            obj.classList.remove(props["cssError"]);
            if (descrDiv)
                while (descrDiv.hasChildNodes()) {
                    descrDiv.removeChild(descrDiv.firstChild);
                }
        };

        this.InputValidator = function (obj, tpl, allowEmpty) {
            obj.addEventListener("keydown", function () {
                self.ClearInput(obj);
            });

            obj.addEventListener("paste", function () {
                self.ClearInput(obj);
            });

            obj.addEventListener("blur", function () {
                self.InputValidate(obj, tpl);
            });
        };

        this.InputValidate = function (obj, tpl, allowEmpty) {
            var hasError = this.NotMatchWithTemplate(obj.value, tpl);

            this.ClearInput(obj);
            if (allowEmpty && !obj.value) return;

            if (hasError) {
                obj.classList.add(props["cssError"]);
                document.getElementById(obj.id + "Validate").innerHTML = hasError;
            }
        };

        this.NotMatchWithTemplate = function (val, tpl) {
            var regexp;

            switch (tpl) {
                case "email":
                    regexp = /^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*@((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$/i;
                    if (!regexp.test(val)) return "Некорректный адрес электронной почты.";
                    else return "";
                case "phone":
                    var regexp = /^\d{12,16}$/i;
                    if (!regexp.test(val)) return "Некорректный номер телефона.";
                    else return "";
                case "password":
                    var regexp = /^(.{0,}(([a-zA-Z][^a-zA-Z])|([^a-zA-Z][a-zA-Z])).{4,})|(.{1,}(([a-zA-Z][^a-zA-Z])|([^a-zA-Z][a-zA-Z])).{3,})|(.{2,}(([a-zA-Z][^a-zA-Z])|([^a-zA-Z][a-zA-Z])).{2,})|(.{3,}(([a-zA-Z][^a-zA-Z])|([^a-zA-Z][a-zA-Z])).{1,})|(.{4,}(([a-zA-Z][^a-zA-Z])|([^a-zA-Z][a-zA-Z])).{0,})$/i;
                    if (!regexp.test(val)) return "Пароль!!!";
                    else return "";
                case "codeword":
                    var regexp = /^[a-zA-Z\d_]{5,15}$/i;
                    if (!regexp.test(val)) return "Только латиница, цифры и символ \'_\'. Не может быть короче 5 и длиннее 15 символов.";
                    else return "";
                default:
                    return "true";
            }
        };
    }

    window.PnB.Validation = window.PnB.Validation || Validation;
})(window);