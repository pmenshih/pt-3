var validate = new PnB.Validation();

document.addEventListener("DOMContentLoaded", function (event) {
    validate.InputValidator(document.getElementById("login"), "email", true);
});
