async function disableBookingIfWorkOrderClosed(executionContext) {
  const formContext = executionContext.getFormContext();
  const workOrderId = formContext
    .getAttribute("cr8c9_fk_work_order")
    .getValue()?.[0]?.id;

  const closedStatus = 976090001;
  if (workOrderId && (await getWorkOrderStatus(workOrderId)) === closedStatus) {
    formContext.ui.controls.forEach((control) => control.setDisabled(true));
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
