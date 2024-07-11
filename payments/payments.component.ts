import { AfterViewInit, Component, Injector, Input, OnInit } from '@angular/core';
import { MessageService } from 'primeng/api';
import { Table } from 'primeng/table';
import { AppComponentBase } from '../../shared/app-component-base';
import { PaymentReportingService, PaymentService, TerminalService } from '../../proxy/castle-service/services';
import { CancelPaymentInput, RefundPaymentInput, SearchPaymentsInput } from '../../proxy/castle-service/dtos/request';
import { ReportingPaymentDto } from '../../proxy/castle-service/dtos/response';
import { TerminalDto } from '../../proxy/castle-service/dtos/terminals';

interface Column {
  field: string;
  header: string;
}

@Component({
  selector: 'payments',
  templateUrl: './payments.component.html',
  styleUrls: ['./payments.scss'],
  providers: [MessageService]
})
export class PaymentsComponent extends AppComponentBase implements OnInit {

  payments: ReportingPaymentDto[] = [];

  paymentsInput: SearchPaymentsInput = {};

  selectedTerminal: TerminalDto = {} as TerminalDto;

  terminals: TerminalDto[] = [];

  selectedTransaction: ReportingPaymentDto[] = [];
  cancelPaymentInput: CancelPaymentInput = {} as CancelPaymentInput;
  refundPaymentInput: RefundPaymentInput = {} as RefundPaymentInput;
  selectedStatus: number;

  successDialog: boolean = false;
  failDialog: boolean = false;
  cancelDialog: boolean = false;
  refundDialog: boolean = false;
  provisionDialog: boolean = false;
  isRowSelected: boolean = false;

  endDate: Date;
  startDate: Date;

  submitted: boolean = false;

  cols!: Column[];
  selectedColumns!: Column[];
  paymentStatuses: any[] = [];


  constructor(injector: Injector, private paymentService: PaymentService, private reportService: PaymentReportingService, private terminalService: TerminalService, private messageService: MessageService) {
    super(injector)
    this.endDate = new Date();
    this.endDate.setHours(23, 59, 0, 0);

    this.startDate = new Date(this.endDate);
    this.startDate.setDate(this.endDate.getDate() - 7);
    this.startDate.setHours(0, 0, 0, 0);


    this.paymentsInput.startTransactionDate = this.startDate.toISOString();
    this.paymentsInput.finishTransactionDate = this.endDate.toISOString();


  }

  ngOnInit() {

    this.terminalService.getAllTerminals().subscribe(response => {
      this.terminals = response
      let all: TerminalDto = {} as TerminalDto;
      all.id = 0;
      all.name = this.l("CastleService::All");
      this.terminals.push(all);
    });

       //this.PaymentReport.searchPayments(this.PaymentsInput).subscribe(data => this.PaymentTerminal = data);

    this.cols = [
      { field: 'installment', header: this.l('CastleService::Installment') },
      { field: 'rrn', header: this.l('CastleService::Rrn') },
      { field: 'terminalName', header: this.l('CastleService::TerminalName') },
      { field: 'cardNumber', header: this.l('CastleService::CardNumber') },
      { field: 'cardBrand', header: this.l('CastleService::CardBrand') },
      { field: 'refundableAmount', header: this.l('CastleService::RefundableAmount') },
      { field: 'paidAmount', header: this.l('CastleService::PaidAmount') },
      { field: 'paymentSource', header: this.l('CastleService::PaymentSource') },
      { field: 'cardType', header: this.l('CastleService::CardType') },
      { field: 'cardAssociation', header: this.l('CastleService::CardAssociation') },
      { field: 'terminalId', header: this.l('CastleService::TerminalId') },
      { field: 'paymentProvider', header: this.l('CastleService::PaymentProvider') },
      { field: 'errorCode', header: this.l('CastleService::ErrorCode') },
      { field: 'errorDescription', header: this.l('CastleService::ErrorDescription') },
      { field: 'merchantCommissionRate', header: this.l('CastleService::MerchantCommissionRate') },
      { field: 'merchantCommissionRateAmount', header: this.l('CastleService::MerchantCommissionRateAmount') },
      { field: 'bankCommissionRate', header: this.l('CastleService::BankCommissionRate') },
      { field: 'bankCommissionRateAmount', header: this.l('CastleService::BankCommissionRateAmount') },
      { field: 'contactName', header: this.l('CastleService::ContactName') },

    ];

    this.selectedColumns = this.cols;

    this.paymentStatuses = [
      { label: 'ALL', value: 2 },
      { label: 'FAILURE', value: 0 },
      { label: 'SUCCESS', value: 1 },

    ];
  }


  dateShow(event) {
    this.paymentsInput.startTransactionDate = this.startDate.toISOString();
    this.paymentsInput.finishTransactionDate = this.endDate.toISOString();
  }

  responseData() {
    console.log(this.selectedTerminal.id);
    console.log(this.selectedStatus);
    this.paymentsInput.terminalId = this.selectedTerminal.id;
    this.paymentsInput.paymentStatus = this.selectedStatus;
    this.reportService.searchPayments(this.paymentsInput).subscribe(data => {
      this.payments = data;
    })
  }

  onGlobalFilter(table: Table, event: Event) {
    table.filterGlobal((event.target as HTMLInputElement).value, 'contains');
  }

  selectRow() {
    const choosen = this.selectedTransaction[this.selectedTransaction.length - 1];
    this.selectedTransaction.length = 0;
    this.selectedTransaction.push(choosen);
    console.log(this.selectedTransaction);
  }

  selectTransactionButton(transaction: any, type: string) {

    if (type === 'cancel') {
      this.cancelDialog = true;
      //this.selectedTransaction = { ...transaction };
      this.cancelPaymentInput.paymentId = this.selectedTransaction[0].id;
      this.cancelPaymentInput.rrn = this.selectedTransaction[0].rrn;
      this.cancelPaymentInput.refundDestinationType = this.selectedTransaction[0].refunds ? this.selectedTransaction[0].refunds[0].refundDestinationType : 1;
    }
    else if (type === 'refund') {
      this.refundDialog = true;
      this.refundPaymentInput.paymentId = this.selectedTransaction[0].id;
      this.refundPaymentInput.rrn = this.selectedTransaction[0].rrn;
      this.refundPaymentInput.refundAmount = this.selectedTransaction[0].amount;
      //this.refundPaymentInput.refundDestinationType = this.selectedTransaction[0].refunds[0].refundDestinationType;
      console.log(this.refundPaymentInput)

    }
    else if (type == 'provision') {
      this.provisionDialog = true;
    }

  }

  //iptal butonu
  async cancel() {
    this.cancelDialog = false;
    console.log('Cancel initiated');
    this.paymentService.cancelPayment(this.cancelPaymentInput).subscribe((res) => {

      console.log('Response received', res);
        if (res.refundType == 0) {
          this.successDialog = true;
          console.log('Success dialog set to true');
        }
        else {
          this.failDialog = true;
          console.log('Fail dialog set to true');
        }
    });
    //this.selectedtransaction = {};
  }

  //iade butonu
  async refund() {
    this.refundDialog = false;
    this.paymentService.refundPayment(this.refundPaymentInput).subscribe((res) => {
      if (res.status == 1) {
        this.successDialog = true;

      }
      else {
        this.failDialog = true;
        console.log("hata2")
      }

    });
  }
  //provizyon kapama butonu

  provision() {
    this.provisionDialog = false;
  }

  getSeverity(status: string) {

    switch (status) {
      case '0':
        return 'danger';
      case '1':
        return 'success';
      case '2':
        return 'warning';
    }
  }
}
