import { Component, OnInit, inject } from '@angular/core';

import { ActivatedRoute, RouterLink } from '@angular/router';

import { TranslatePipe } from '@commerce/localization';



@Component({

  standalone: true,

  imports: [RouterLink, TranslatePipe],

  template: `

    <section class="payment-page" aria-labelledby="payment-success-title">

      <h1 id="payment-success-title">{{ 'payment.success.title' | translate }}</h1>

      <p>{{ 'payment.success.message' | translate }}</p>



      @if (orderNumber) {

        <p class="order-number">{{ orderNumber }}</p>

        <div class="actions">

          <a [routerLink]="['/order-confirmation', orderNumber]" [queryParams]="accessToken ? { accessToken } : null">

            {{ 'payment.success.viewOrder' | translate }}

          </a>

          <a routerLink="/">{{ 'nav.home' | translate }}</a>

          <a routerLink="/products">{{ 'cart.continueShopping' | translate }}</a>

        </div>

      } @else {

        <div class="actions">

          <a routerLink="/">{{ 'nav.home' | translate }}</a>

        </div>

      }

    </section>

  `,

  styles: [`

    .payment-page { display: grid; gap: 1rem; max-width: 32rem; }

    .order-number { font-size: 1.25rem; font-weight: 700; }

    .actions { display: flex; gap: 1rem; flex-wrap: wrap; }

    .actions a { color: var(--primary, #0f766e); }

  `]

})

export class PaymentSuccessPageComponent implements OnInit {

  private readonly route = inject(ActivatedRoute);



  orderNumber = '';

  accessToken: string | null = null;



  ngOnInit(): void {

    const params = this.route.snapshot.queryParamMap;

    this.orderNumber = params.get('orderNumber') ?? '';

    this.accessToken = params.get('accessToken');

  }

}


