const formatToCurrency = amount => {  
    return amount.toFixed(1).replace(/\d(?=(\d{3})+\.)/g, "$&,");
};

$('#searchFromDate').datepicker({
    format: 'dd/mm/yyyy',
    autoUpdateInput: false,
    singleDatePicker: true,
    todayHighlight: true,
    autoclose: true
});

$('#searchToDate').datepicker({
    format: 'dd/mm/yyyy',
    autoUpdateInput: false,
    todayHighlight: true,
    singleDatePicker: true,
    autoclose: true
});



