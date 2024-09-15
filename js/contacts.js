function fullNameConcat(executionContext) {
  const formContext = executionContext.getFormContext();
  const firstName = formContext
    .getAttribute("cr8c9_slot_first_name")
    .getValue();
  const lastName = formContext.getAttribute("cr8c9_slot_last_name").getValue();
  const fullName = `${firstName || ""} ${lastName || ""}`.trim();
  formContext.getAttribute("cr8c9_name").setValue(fullName);
}

function validateEmail(executionContext) {
  const formContext = executionContext.getFormContext();
  const email = formContext.getAttribute("cr8c9_slot_email").getValue();
  const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  if (!emailPattern.test(email)) {
    formContext
      .getControl("cr8c9_slot_email")
      .setNotification("Please enter a valid email address.");
    return false;
  } else {
    formContext.getControl("cr8c9_slot_email").clearNotification();
    return true;
  }
}

function validateMobile(executionContext) {
  const formContext = executionContext.getFormContext();
  const mobile = formContext.getAttribute("cr8c9_slot_mobile").getValue();
  const mobilePattern = /^[0-9]{10,15}$/;
  if (!mobilePattern.test(mobile)) {
    formContext
      .getControl("cr8c9_slot_mobile")
      .setNotification("Please enter a valid mobile number (10-15 digits).");
    return false;
  } else {
    formContext.getControl("cr8c9_slot_mobile").clearNotification();
    return true;
  }
}
