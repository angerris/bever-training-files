async function disableBookingIfWorkOrderClosed(executionContext) {
  const formContext = executionContext.getFormContext();
  const workOrderLookup = formContext
    .getAttribute("cr8c9_fk_work_order")
    .getValue();

  if (workOrderLookup) {
    const workOrderId = workOrderLookup[0].id;
    const status = await getWorkOrderStatus(workOrderId);
    if (status === 976090001) {
      formContext.ui.controls.forEach(function (control) {
        control.setDisabled(true);
      });
    }
  }
}

async function getWorkOrderStatus(workOrderId) {
  const result = await Xrm.WebApi.retrieveRecord(
    "cr8c9_work_order",
    workOrderId.replace(/[{}]/g, ""),
    "?$select=cr8c9_os_status"
  );
  return result.cr8c9_os_status;
}
