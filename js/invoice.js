function setAllFieldsReadOnly(executionContext) {
  const formContext = executionContext.getFormContext();
  const attributes = formContext.data.entity.attributes.get();
  attributes.forEach(function (attribute) {
    const control = formContext.getControl(attribute.getName());
    if (control) {
      control.setDisabled(true);
    }
  });
}
