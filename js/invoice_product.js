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

function setAllFieldsReadOnly(executionContext) {
  const formContext = executionContext.getFormContext();
  const attributes = formContext.data.entity.attributes.get();

  attributes.forEach((attribute) => {
    const control = formContext.getControl(attribute.getName());
    if (control) control.setDisabled(true);
  });
}

function copyProductValueToName(executionContext) {
  const formContext = executionContext.getFormContext();
  const productValue = formContext.getAttribute("cr8c9_fk_product").getValue();
  if (productValue) {
    formContext.getAttribute("cr8c9_name").setValue(productValue[0].name);
  } else {
    formContext.getAttribute("cr8c9_name").setValue(null);
  }
}

function onLoad(executionContext) {
  setAllFieldsReadOnly(executionContext);
  hideFieldsBasedOnCondition(executionContext);
  copyProductValueToName(executionContext);
}
