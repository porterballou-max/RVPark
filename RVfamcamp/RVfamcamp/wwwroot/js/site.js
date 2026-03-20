// script to unlock the payments, reservations, profile pages after submit has been clicked
// on the login page
document.getElementById("loginLink").addEventListener("click", function() {
    doucment.getElementById("paymentsLink").removeAttribute("disabled");
    document.getElementById("reservationsLink").removeAttribute("disabled");
    document.getElementById("profileLink").removeAttribute("disabled");
});