function hideFieldsBasedOnCondition(executionContext) {
  const formContext = executionContext.getFormContext();
  const durationVisible = !!formContext
    .getAttribute("cr8c9_int_duration")
    .getValue();
  const quantityVisible = !!formContext
    .getAttribute("cr8c9_int_quantity")
    .getValue();

  formContext
    .getControl("cr8c9_int_duration")
    .setVisible(durationVisible || !quantityVisible);
  formContext
    .getControl("cr8c9_int_quantity")
    .setVisible(quantityVisible || !durationVisible);
}
