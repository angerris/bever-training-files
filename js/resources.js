function fullNameConcat(executionContext) {
  const formContext = executionContext.getFormContext();
  const firstName = formContext
    .getAttribute("cr8c9_slot_first_name")
    .getValue();
  const lastName = formContext.getAttribute("cr8c9_slot_last_name").getValue();
  const fullName = `${firstName || ""} ${lastName || ""}`.trim();
  formContext.getAttribute("cr8c9_name").setValue(fullName);
}
